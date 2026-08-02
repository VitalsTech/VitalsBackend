using PrescriptionService.Application.DTOs;
using PrescriptionService.Application.Interfaces;
using PrescriptionService.Domain.Entities;
using PrescriptionService.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace PrescriptionService.Application.Services;

public sealed class LabOrderAppService : ILabOrderService
{
    private readonly ILabOrderRepository _repo;
    private readonly IMedicalRecordEventClient _medicalEvents;
    private readonly ILogger<LabOrderAppService> _logger;

    public LabOrderAppService(
        ILabOrderRepository repo,
        IMedicalRecordEventClient medicalEvents,
        ILogger<LabOrderAppService> logger)
    {
        _repo = repo;
        _medicalEvents = medicalEvents;
        _logger = logger;
    }

    public async Task<LabOrderResponse> CreateAsync(
        Guid doctorId,
        CreateLabOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.PatientId == Guid.Empty)
            throw new InvalidOperationException("patientId обязателен.");

        var items = (request.Items ?? Array.Empty<LabOrderItemDto>())
            .Where(i => !string.IsNullOrWhiteSpace(i.TestName))
            .ToList();

        if (items.Count == 0)
            throw new InvalidOperationException("Нужен хотя бы один анализ.");

        var now = DateTime.UtcNow;
        var priority = string.IsNullOrWhiteSpace(request.Priority) ? "routine" : request.Priority.Trim().ToLowerInvariant();
        if (priority is not ("routine" or "urgent"))
            priority = "routine";

        var order = new LabOrder
        {
            Id = Guid.NewGuid(),
            PatientId = request.PatientId,
            DoctorId = doctorId,
            ConsultationId = request.ConsultationId,
            Status = LabOrderStatus.Ordered,
            OrderedAt = now,
            ClinicalIndication = TrimOrNull(request.ClinicalIndication),
            Priority = priority,
            DoctorComment = TrimOrNull(request.DoctorComment),
            CreatedAt = now,
            UpdatedAt = now,
            Items = items.Select(i => new LabOrderItem
            {
                Id = Guid.NewGuid(),
                TestName = i.TestName.Trim(),
                TestCode = TrimOrNull(i.TestCode),
                SpecimenType = TrimOrNull(i.SpecimenType),
                SpecialInstructions = TrimOrNull(i.SpecialInstructions)
            }).ToList()
        };

        await _repo.SaveAsync(order, cancellationToken);
        await AddHistoryAsync(order.Id, LabOrderStatus.Ordered, LabOrderStatus.Ordered, "doctor", "created", cancellationToken);

        await _medicalEvents.AppendEventAsync(order.PatientId, "DocumentUploaded", new
        {
            title = "Направление на анализы",
            documentType = "lab-order",
            labOrderId = order.Id,
            consultationId = order.ConsultationId,
            priority = order.Priority,
            tests = order.Items.Select(i => i.TestName).ToArray(),
            source = "prescription-service"
        }, order.Id, cancellationToken);

        foreach (var item in order.Items)
        {
            await _medicalEvents.AppendEventAsync(order.PatientId, "LabResultReceived", new
            {
                testName = item.TestName,
                resultValue = "Назначено",
                referenceRange = (string?)null,
                isCritical = false,
                labOrderId = order.Id,
                labOrderItemId = item.Id,
                source = "lab-order"
            }, order.Id, cancellationToken);
        }

        _logger.LogInformation("Lab order {LabOrderId} created for patient {PatientId}", order.Id, order.PatientId);
        return Map(order);
    }

    public async Task<LabOrderResponse> GetAsync(Guid labOrderId, CancellationToken cancellationToken = default)
    {
        var order = await _repo.GetByIdAsync(labOrderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Lab order {labOrderId} not found.");
        return Map(order);
    }

    public async Task<IReadOnlyList<LabOrderResponse>> GetPatientOrdersAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var orders = await _repo.GetByPatientIdAsync(patientId, cancellationToken);
        return orders.Select(Map).ToList();
    }

    public async Task<LabOrderResponse> StartAsync(
        Guid labOrderId,
        Guid userId,
        StartLabOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var order = await _repo.GetByIdForUpdateAsync(labOrderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Lab order {labOrderId} not found.");

        if (order.Status is LabOrderStatus.Completed or LabOrderStatus.Cancelled)
            throw new InvalidOperationException($"Нельзя взять в работу заказ в статусе {order.Status}.");

        if (order.Status == LabOrderStatus.InProgress)
            return Map(order);

        var from = order.Status;
        order.Status = LabOrderStatus.InProgress;
        order.ExternalLabOrderId = TrimOrNull(request.ExternalLabOrderId) ?? order.ExternalLabOrderId;
        order.UpdatedAt = DateTime.UtcNow;
        await _repo.SaveAsync(order, cancellationToken);
        await AddHistoryAsync(order.Id, from, order.Status, "lab", null, cancellationToken);

        return Map(order);
    }

    public async Task<LabOrderResponse> CancelAsync(
        Guid labOrderId,
        Guid userId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var order = await _repo.GetByIdForUpdateAsync(labOrderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Lab order {labOrderId} not found.");

        if (order.Status is LabOrderStatus.Completed or LabOrderStatus.Cancelled)
            throw new InvalidOperationException($"Нельзя отменить заказ в статусе {order.Status}.");

        var from = order.Status;
        order.Status = LabOrderStatus.Cancelled;
        order.CancelReason = string.IsNullOrWhiteSpace(reason) ? "Отменено" : reason.Trim();
        order.UpdatedAt = DateTime.UtcNow;
        await _repo.SaveAsync(order, cancellationToken);
        await AddHistoryAsync(order.Id, from, order.Status, "user", order.CancelReason, cancellationToken);

        await _medicalEvents.AppendEventAsync(order.PatientId, "DocumentUploaded", new
        {
            title = "Направление на анализы отменено",
            documentType = "lab-order-cancelled",
            labOrderId = order.Id,
            reason = order.CancelReason,
            source = "prescription-service"
        }, order.Id, cancellationToken);

        return Map(order);
    }

    public async Task<LabOrderResponse> RecordResultsAsync(
        Guid labOrderId,
        RecordLabOrderResultsRequest request,
        CancellationToken cancellationToken = default)
    {
        var order = await _repo.GetByIdForUpdateAsync(labOrderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Lab order {labOrderId} not found.");

        if (order.Status == LabOrderStatus.Cancelled)
            throw new InvalidOperationException("Нельзя записать результаты в отменённый заказ.");

        var now = DateTime.UtcNow;
        var results = request.Results ?? Array.Empty<LabOrderResultItemDto>();
        if (results.Count == 0)
            throw new InvalidOperationException("Нужен хотя бы один результат.");

        foreach (var result in results)
        {
            var item = order.Items.FirstOrDefault(i => i.Id == result.ItemId)
                ?? throw new KeyNotFoundException($"Пункт анализа {result.ItemId} не найден в заказе.");

            item.ResultValue = TrimOrNull(result.ResultValue);
            item.ReferenceRange = TrimOrNull(result.ReferenceRange);
            item.Unit = TrimOrNull(result.Unit);
            item.IsCritical = result.IsCritical;
            item.ResultAttachmentUrl = TrimOrNull(result.ResultAttachmentUrl);
            item.ResultComment = TrimOrNull(result.ResultComment);
            item.ResultReceivedAt = now;

            await _medicalEvents.AppendEventAsync(order.PatientId, "LabResultReceived", new
            {
                testName = item.TestName,
                resultValue = item.ResultValue ?? string.Empty,
                referenceRange = item.ReferenceRange,
                unit = item.Unit,
                isCritical = item.IsCritical,
                labOrderId = order.Id,
                labOrderItemId = item.Id,
                comment = item.ResultComment,
                source = "lab-order-result"
            }, order.Id, cancellationToken);
        }

        var from = order.Status;
        var allHaveResults = order.Items.All(i => !string.IsNullOrWhiteSpace(i.ResultValue));
        if (request.MarkCompleted || allHaveResults)
        {
            order.Status = LabOrderStatus.Completed;
            order.CompletedAt = now;
        }
        else if (order.Status == LabOrderStatus.Ordered)
        {
            order.Status = LabOrderStatus.InProgress;
        }

        order.UpdatedAt = now;
        await _repo.SaveAsync(order, cancellationToken);

        if (from != order.Status)
            await AddHistoryAsync(order.Id, from, order.Status, "lab", "results recorded", cancellationToken);

        return Map(order);
    }

    private async Task AddHistoryAsync(
        Guid orderId,
        LabOrderStatus from,
        LabOrderStatus to,
        string initiator,
        string? reason,
        CancellationToken cancellationToken) =>
        await _repo.AddStatusHistoryAsync(new LabOrderStatusHistory
        {
            Id = Guid.NewGuid(),
            LabOrderId = orderId,
            FromStatus = from,
            ToStatus = to,
            Initiator = initiator,
            Reason = reason,
            OccurredAt = DateTime.UtcNow
        }, cancellationToken);

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static LabOrderResponse Map(LabOrder order) => new()
    {
        LabOrderId = order.Id,
        PatientId = order.PatientId,
        DoctorId = order.DoctorId,
        ConsultationId = order.ConsultationId,
        Status = order.Status.ToString(),
        OrderedAt = order.OrderedAt,
        ClinicalIndication = order.ClinicalIndication,
        Priority = order.Priority,
        DoctorComment = order.DoctorComment,
        ExternalLabOrderId = order.ExternalLabOrderId,
        CancelReason = order.CancelReason,
        CompletedAt = order.CompletedAt,
        UpdatedAt = order.UpdatedAt,
        Items = order.Items.Select(i => new LabOrderItemDto
        {
            ItemId = i.Id,
            TestName = i.TestName,
            TestCode = i.TestCode,
            SpecimenType = i.SpecimenType,
            SpecialInstructions = i.SpecialInstructions,
            ResultValue = i.ResultValue,
            ReferenceRange = i.ReferenceRange,
            Unit = i.Unit,
            IsCritical = i.IsCritical,
            ResultReceivedAt = i.ResultReceivedAt,
            ResultAttachmentUrl = i.ResultAttachmentUrl,
            ResultComment = i.ResultComment
        }).ToList()
    };
}
