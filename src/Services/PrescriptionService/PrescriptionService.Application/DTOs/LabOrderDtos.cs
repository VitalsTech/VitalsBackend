namespace PrescriptionService.Application.DTOs;

public sealed class LabOrderItemDto
{
    public Guid? ItemId { get; set; }
    public string TestName { get; set; } = string.Empty;
    public string? TestCode { get; set; }
    public string? SpecimenType { get; set; }
    public string? SpecialInstructions { get; set; }
    public string? ResultValue { get; set; }
    public string? ReferenceRange { get; set; }
    public string? Unit { get; set; }
    public bool IsCritical { get; set; }
    public DateTime? ResultReceivedAt { get; set; }
    public string? ResultAttachmentUrl { get; set; }
    public string? ResultComment { get; set; }
}

public class CreateLabOrderRequest
{
    public Guid PatientId { get; set; }
    public Guid? ConsultationId { get; set; }
    public string? ClinicalIndication { get; set; }
    /// <summary>routine | urgent</summary>
    public string Priority { get; set; } = "routine";
    public string? DoctorComment { get; set; }
    public IReadOnlyList<LabOrderItemDto> Items { get; set; } = Array.Empty<LabOrderItemDto>();
}

/// <summary>Internal create — doctorId из тела (без JWT).</summary>
public sealed class InternalCreateLabOrderRequest : CreateLabOrderRequest
{
    public Guid DoctorId { get; set; }
}

public sealed class LabOrderResponse
{
    public Guid LabOrderId { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid? ConsultationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime OrderedAt { get; set; }
    public string? ClinicalIndication { get; set; }
    public string Priority { get; set; } = "routine";
    public string? DoctorComment { get; set; }
    public string? ExternalLabOrderId { get; set; }
    public string? CancelReason { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public IReadOnlyList<LabOrderItemDto> Items { get; set; } = Array.Empty<LabOrderItemDto>();
}

public sealed class CancelLabOrderRequest
{
    public string Reason { get; set; } = string.Empty;
}

public sealed class StartLabOrderRequest
{
    public string? ExternalLabOrderId { get; set; }
}

public sealed class LabOrderResultItemDto
{
    public Guid ItemId { get; set; }
    public string? ResultValue { get; set; }
    public string? ReferenceRange { get; set; }
    public string? Unit { get; set; }
    public bool IsCritical { get; set; }
    public string? ResultAttachmentUrl { get; set; }
    public string? ResultComment { get; set; }
}

public sealed class RecordLabOrderResultsRequest
{
    /// <summary>Если true — заказ Completed даже при частичных результатах.</summary>
    public bool MarkCompleted { get; set; } = true;
    public IReadOnlyList<LabOrderResultItemDto> Results { get; set; } = Array.Empty<LabOrderResultItemDto>();
}
