using MedicalRecordService.Application.DTOs;

namespace MedicalRecordService.Application.Interfaces;

public interface IAccessControlService
{
    Task EnsureAccessAsync(Guid patientId, ActorContext actor, string requiredScope, CancellationToken cancellationToken = default);
    bool IsPatientSelf(Guid patientId, ActorContext actor);
}

public interface IMedicalEventService
{
    Task<AppendEventResponse> AppendEventAsync(Guid patientId, AppendEventRequest request, ActorContext actor, CancellationToken cancellationToken = default);
}

public interface IPatientRecordQueryService
{
    Task<PatientHistoryResponse> GetHistoryAsync(Guid patientId, DateTime? from, DateTime? to, IReadOnlyList<string>? eventTypes, ActorContext actor, CancellationToken cancellationToken = default);
    Task<PatientCurrentStateDto> GetCurrentStateAsync(Guid patientId, ActorContext actor, CancellationToken cancellationToken = default);
}

public interface IAccessGrantService
{
    Task<AccessGrantDto> CreateGrantAsync(Guid patientId, CreateAccessGrantRequest request, ActorContext actor, CancellationToken cancellationToken = default);
    Task RevokeGrantAsync(Guid patientId, Guid grantId, ActorContext actor, CancellationToken cancellationToken = default);
}

public interface IPatientAttachmentService
{
    Task<PatientAttachmentDto> UploadAsync(
        Guid patientId,
        string fileName,
        Stream content,
        string contentType,
        ActorContext actor,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PatientAttachmentDto>> ListAsync(
        Guid patientId,
        ActorContext actor,
        CancellationToken cancellationToken = default);
}
