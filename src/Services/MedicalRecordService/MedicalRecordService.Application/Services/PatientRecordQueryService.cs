using System.Text.Json;
using MedicalRecordService.Application.DTOs;
using MedicalRecordService.Application.Exceptions;
using MedicalRecordService.Application.Interfaces;
using MedicalRecordService.Domain.Entities;
using MedicalRecordService.Domain.Enums;
using MedicalRecordService.Domain.Interfaces;

namespace MedicalRecordService.Application.Services;

public sealed class PatientRecordQueryService : IPatientRecordQueryService
{
    private readonly IMedicalEventRepository _events;
    private readonly IPatientSnapshotRepository _snapshots;
    private readonly IProjectionRepository _projections;
    private readonly IAccessControlService _accessControl;
    private readonly IAuditLogRepository _audit;

    public PatientRecordQueryService(
        IMedicalEventRepository events,
        IPatientSnapshotRepository snapshots,
        IProjectionRepository projections,
        IAccessControlService accessControl,
        IAuditLogRepository audit)
    {
        _events = events;
        _snapshots = snapshots;
        _projections = projections;
        _accessControl = accessControl;
        _audit = audit;
    }

    public async Task<PatientCurrentStateDto> GetCurrentStateAsync(
        Guid patientId,
        ActorContext actor,
        CancellationToken cancellationToken = default)
    {
        await _accessControl.EnsureAccessAsync(patientId, actor, AccessScopes.ReadProjections, cancellationToken);
        await LogReadAsync(patientId, actor, "current-state", cancellationToken);

        var state = await _projections.GetCurrentStateAsync(patientId, cancellationToken);
        return PatientRecordMapper.ToDto(state);
    }

    public async Task<PatientHistoryResponse> GetHistoryAsync(
        Guid patientId,
        DateTime? from,
        DateTime? to,
        IReadOnlyList<string>? eventTypes,
        ActorContext actor,
        CancellationToken cancellationToken = default)
    {
        await _accessControl.EnsureAccessAsync(patientId, actor, AccessScopes.ReadHistory, cancellationToken);
        await LogReadAsync(patientId, actor, "history", cancellationToken);

        var snapshot = await _snapshots.GetLatestAsync(patientId, cancellationToken);
        PatientCurrentStateDto currentState;

        currentState = PatientRecordMapper.ToDto(await _projections.GetCurrentStateAsync(patientId, cancellationToken));
        _ = snapshot;

        var events = await _events.GetEventsAsync(patientId, from, to, eventTypes, cancellationToken);

        return new PatientHistoryResponse
        {
            PatientId = patientId,
            SnapshotVersion = snapshot?.UpToVersion,
            CurrentState = currentState,
            Events = events.Select(MapEvent).ToList()
        };
    }

    private async Task LogReadAsync(Guid patientId, ActorContext actor, string resource, CancellationToken cancellationToken)
    {
        await _audit.AddAsync(new AuditLogEntry
        {
            PatientId = patientId,
            ActorUserId = actor.UserId,
            ActorType = actor.IsSystemService ? actor.ServiceName ?? "Service" : "User",
            ActionType = AuditActionTypes.Read,
            DetailsJson = JsonSerializer.Serialize(new { resource }),
            IpAddress = actor.IpAddress,
            SessionId = actor.SessionId
        }, cancellationToken);
        await _audit.SaveChangesAsync(cancellationToken);
    }

    private static MedicalEventDto MapEvent(MedicalEvent e) => new()
    {
        EventId = e.EventId,
        EventType = e.EventType,
        Version = e.Version,
        Payload = JsonDocument.Parse(e.PayloadJson).RootElement,
        OccurredAt = e.OccurredAt,
        SourceService = e.SourceService,
        CorrelationId = e.CorrelationId,
        ActorUserId = e.ActorUserId,
        ActorRole = e.ActorRole
    };
}
