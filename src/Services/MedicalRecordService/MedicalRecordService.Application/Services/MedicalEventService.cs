using System.Text.Json;
using MedicalRecordService.Application.DTOs;
using MedicalRecordService.Application.Exceptions;
using MedicalRecordService.Application.Interfaces;
using MedicalRecordService.Application.Options;
using MedicalRecordService.Domain.Entities;
using MedicalRecordService.Domain.Enums;
using MedicalRecordService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MedicalRecordService.Application.Services;

public sealed class MedicalEventService : IMedicalEventService
{
    private readonly IMedicalEventRepository _events;
    private readonly IPatientSnapshotRepository _snapshots;
    private readonly ProjectionUpdater _projectionUpdater;
    private readonly IAccessControlService _accessControl;
    private readonly IAuditLogRepository _audit;
    private readonly IProjectionRepository _projections;
    private readonly IMedicalRecordEventPublisher _publisher;
    private readonly IDoctorRecipientResolver _doctorRecipients;
    private readonly MedicalRecordOptions _options;
    private readonly ILogger<MedicalEventService> _logger;

    public MedicalEventService(
        IMedicalEventRepository events,
        IPatientSnapshotRepository snapshots,
        IProjectionRepository projections,
        ProjectionUpdater projectionUpdater,
        IAccessControlService accessControl,
        IAuditLogRepository audit,
        IMedicalRecordEventPublisher publisher,
        IDoctorRecipientResolver doctorRecipients,
        IOptions<MedicalRecordOptions> options,
        ILogger<MedicalEventService> logger)
    {
        _events = events;
        _snapshots = snapshots;
        _projections = projections;
        _projectionUpdater = projectionUpdater;
        _accessControl = accessControl;
        _audit = audit;
        _publisher = publisher;
        _doctorRecipients = doctorRecipients;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AppendEventResponse> AppendEventAsync(
        Guid patientId,
        AppendEventRequest request,
        ActorContext actor,
        CancellationToken cancellationToken = default)
    {
        await _accessControl.EnsureAccessAsync(patientId, actor, AccessScopes.WriteEvents, cancellationToken);

        var existing = await _events.GetByEventIdAsync(request.EventId, cancellationToken);
        if (existing is not null)
        {
            if (existing.PatientId != patientId)
                throw new MedicalRecordValidationException("Event id belongs to another patient.");

            return new AppendEventResponse
            {
                EventId = existing.EventId,
                Version = existing.Version,
                OccurredAt = existing.OccurredAt
            };
        }

        var version = await _events.GetLatestVersionAsync(patientId, cancellationToken) + 1;
        var eventType = MedicalEventTypes.NormalizeAppendEventType(request.EventType);
        var payload = MedicalEventTypes.NormalizeAppendPayload(eventType, request.Payload);
        var medicalEvent = new MedicalEvent
        {
            EventId = request.EventId,
            PatientId = patientId,
            EventType = eventType,
            Version = version,
            PayloadJson = payload.GetRawText(),
            SourceService = request.SourceService,
            CorrelationId = request.CorrelationId,
            ActorUserId = actor.UserId,
            ActorRole = actor.Roles.FirstOrDefault()
        };

        await _events.AddAsync(medicalEvent, cancellationToken);
        await _events.SaveChangesAsync(cancellationToken);

        await _projectionUpdater.ApplyAsync(medicalEvent, cancellationToken);
        await TryCreateSnapshotAsync(patientId, version, cancellationToken);

        await _audit.AddAsync(new AuditLogEntry
        {
            PatientId = patientId,
            ActorUserId = actor.UserId,
            ActorType = actor.IsSystemService ? actor.ServiceName ?? "Service" : "User",
            ActionType = AuditActionTypes.Create,
            MedicalEventId = medicalEvent.EventId,
            DetailsJson = JsonSerializer.Serialize(new { request.EventType, version }),
            IpAddress = actor.IpAddress,
            SessionId = actor.SessionId
        }, cancellationToken);
        await _audit.SaveChangesAsync(cancellationToken);

        await _publisher.PublishEventAppendedAsync(
            patientId,
            medicalEvent.EventId,
            medicalEvent.EventType,
            version,
            medicalEvent.PayloadJson,
            cancellationToken);

        await TryNotifyDoctorsAsync(medicalEvent, cancellationToken);

        return new AppendEventResponse
        {
            EventId = medicalEvent.EventId,
            Version = version,
            OccurredAt = medicalEvent.OccurredAt
        };
    }

    private async Task TryNotifyDoctorsAsync(MedicalEvent medicalEvent, CancellationToken cancellationToken)
    {
        try
        {
            if (medicalEvent.EventType.Equals(MedicalEventTypes.MoodCheck, StringComparison.OrdinalIgnoreCase) ||
                (medicalEvent.EventType.Equals(MedicalEventTypes.VitalSignRecorded, StringComparison.OrdinalIgnoreCase)
                 && IsMoodVital(medicalEvent.PayloadJson)))
            {
                await NotifyMoodAsync(medicalEvent, cancellationToken);
                return;
            }

            if (medicalEvent.EventType.Equals(MedicalEventTypes.AiTriageUrgencyDetermined, StringComparison.OrdinalIgnoreCase))
                await NotifyTriageAsync(medicalEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish doctor notifications for event {EventId} ({EventType})",
                medicalEvent.EventId,
                medicalEvent.EventType);
        }
    }

    private async Task NotifyMoodAsync(MedicalEvent medicalEvent, CancellationToken cancellationToken)
    {
        using var doc = JsonDocument.Parse(medicalEvent.PayloadJson);
        var root = doc.RootElement;

        var notifyDoctor = GetBool(root, "notifyDoctor") ?? true;
        if (!notifyDoctor)
            return;

        var mood = GetString(root, "mood") ?? GetString(root, "value") ?? "отметка";
        var moodCode = GetString(root, "moodCode") ?? "good";
        var severity = GetInt(root, "severity") ?? (moodCode == "worse" ? 3 : moodCode == "tired" ? 2 : 1);
        var escalate = GetBool(root, "escalate") ?? moodCode.Equals("worse", StringComparison.OrdinalIgnoreCase);

        var doctors = await _doctorRecipients.ResolveDoctorIdsAsync(medicalEvent.PatientId, cancellationToken);
        if (doctors.Count == 0)
            return;

        var priority = escalate || severity >= 3 ? "High" : "Medium";
        var data = new Dictionary<string, string>
        {
            ["patient_id"] = medicalEvent.PatientId.ToString(),
            ["medical_event_id"] = medicalEvent.EventId.ToString(),
            ["mood"] = mood,
            ["mood_code"] = moodCode,
            ["severity"] = severity.ToString(),
            ["deep_link"] = $"/doctor/patients/{medicalEvent.PatientId}",
            ["title"] = "Самочувствие пациента",
            ["message"] = $"Пациент отметил: «{mood}»"
        };

        _logger.LogInformation(
            "Publishing mood notify patientId={PatientId} moodCode={MoodCode} severity={Severity} recipients={Count}",
            medicalEvent.PatientId,
            moodCode,
            severity,
            doctors.Count);

        await _publisher.PublishPatientMoodUpdatedAsync(
            medicalEvent.PatientId,
            medicalEvent.EventId,
            doctors,
            data,
            priority,
            cancellationToken);
    }

    private async Task NotifyTriageAsync(MedicalEvent medicalEvent, CancellationToken cancellationToken)
    {
        using var doc = JsonDocument.Parse(medicalEvent.PayloadJson);
        var root = doc.RootElement;

        var urgency = GetInt(root, "urgencyLevel") ?? 2;
        var specialization = GetString(root, "recommendedSpecialization") ?? "специалист";
        var recommendation = GetString(root, "recommendation") ?? "";
        var sessionId = GetString(root, "sessionId") ?? medicalEvent.EventId.ToString();
        var urgencyLabel = urgency >= 5 ? "Срочно" : urgency >= 4 ? "Скоро" : "Плановая";

        var doctors = await _doctorRecipients.ResolveDoctorIdsAsync(medicalEvent.PatientId, cancellationToken);
        if (doctors.Count == 0)
            return;

        var priority = urgency >= 4 ? "High" : "Medium";
        var data = new Dictionary<string, string>
        {
            ["patient_id"] = medicalEvent.PatientId.ToString(),
            ["medical_event_id"] = medicalEvent.EventId.ToString(),
            ["session_id"] = sessionId,
            ["urgency_level"] = urgency.ToString(),
            ["urgency_label"] = urgencyLabel,
            ["recommended_specialization"] = specialization,
            ["recommendation"] = recommendation,
            ["deep_link"] = $"/doctor/patients/{medicalEvent.PatientId}",
            ["title"] = "Новый результат ИИ-триажа",
            ["message"] = $"Срочность: {urgencyLabel} · рекомендован {specialization.ToLowerInvariant()}"
        };

        await _publisher.PublishPatientTriageCompletedAsync(
            medicalEvent.PatientId,
            medicalEvent.EventId,
            doctors,
            data,
            priority,
            cancellationToken);
    }

    private static bool IsMoodVital(string payloadJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            var vitalType = GetString(doc.RootElement, "vitalType");
            return string.IsNullOrEmpty(vitalType)
                   || vitalType.Equals("mood", StringComparison.OrdinalIgnoreCase)
                   || doc.RootElement.TryGetProperty("mood", out _);
        }
        catch
        {
            return false;
        }
    }

    private async Task TryCreateSnapshotAsync(Guid patientId, long version, CancellationToken cancellationToken)
    {
        if (_options.SnapshotEveryEvents <= 0 || version % _options.SnapshotEveryEvents != 0)
            return;

        var state = PatientRecordMapper.ToDto(await _projections.GetCurrentStateAsync(patientId, cancellationToken));

        var snapshot = new PatientSnapshot
        {
            PatientId = patientId,
            UpToVersion = version,
            StateJson = JsonSerializer.Serialize(state)
        };

        await _snapshots.AddAsync(snapshot, cancellationToken);
        await _snapshots.SaveChangesAsync(cancellationToken);
    }

    private static string? GetString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : root.TryGetProperty(name, out value) && value.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False
                ? value.ToString()
                : null;

    private static int? GetInt(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value))
            return null;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var n))
            return n;
        if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out n))
            return n;
        return null;
    }

    private static bool? GetBool(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value))
            return null;
        if (value.ValueKind is JsonValueKind.True or JsonValueKind.False)
            return value.GetBoolean();
        if (value.ValueKind == JsonValueKind.String && bool.TryParse(value.GetString(), out var b))
            return b;
        return null;
    }
}
