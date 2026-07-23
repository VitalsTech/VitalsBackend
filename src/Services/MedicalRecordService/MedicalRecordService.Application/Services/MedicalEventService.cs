using System.Text.Json;
using MedicalRecordService.Application.DTOs;
using MedicalRecordService.Application.Exceptions;
using MedicalRecordService.Application.Interfaces;
using MedicalRecordService.Application.Options;
using MedicalRecordService.Domain.Entities;
using MedicalRecordService.Domain.Enums;
using MedicalRecordService.Domain.Interfaces;
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
    private readonly MedicalRecordOptions _options;

    public MedicalEventService(
        IMedicalEventRepository events,
        IPatientSnapshotRepository snapshots,
        IProjectionRepository projections,
        ProjectionUpdater projectionUpdater,
        IAccessControlService accessControl,
        IAuditLogRepository audit,
        IMedicalRecordEventPublisher publisher,
        IOptions<MedicalRecordOptions> options)
    {
        _events = events;
        _snapshots = snapshots;
        _projections = projections;
        _projectionUpdater = projectionUpdater;
        _accessControl = accessControl;
        _audit = audit;
        _publisher = publisher;
        _options = options.Value;
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

        await _publisher.PublishEventAppendedAsync(patientId, medicalEvent.EventId, medicalEvent.EventType, version, cancellationToken);

        return new AppendEventResponse
        {
            EventId = medicalEvent.EventId,
            Version = version,
            OccurredAt = medicalEvent.OccurredAt
        };
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
}
