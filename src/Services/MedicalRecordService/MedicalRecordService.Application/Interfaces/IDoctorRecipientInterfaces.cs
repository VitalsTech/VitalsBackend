using MedicalRecordService.Domain.Entities;

namespace MedicalRecordService.Application.Interfaces;

public interface IDoctorRecipientResolver
{
    /// <summary>
    /// True if any of the doctor identity ids has an active grant or is the latest consultation doctor.
    /// </summary>
    Task<bool> DoctorHasAccessAsync(
        Guid patientId,
        IReadOnlyList<Guid> doctorIdentityIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Active access-grant doctors ∪ last consultation doctor (PublicId).
    /// </summary>
    Task<IReadOnlyList<Guid>> ResolveDoctorIdsAsync(Guid patientId, CancellationToken cancellationToken = default);
}

public interface IConsultationDoctorClient
{
    Task<Guid?> GetLatestDoctorIdAsync(Guid patientId, CancellationToken cancellationToken = default);
}

public interface IMedicalRecordEventPublisher
{
    Task PublishEventAppendedAsync(
        Guid patientId,
        Guid eventId,
        string eventType,
        long version,
        string? payloadJson = null,
        CancellationToken cancellationToken = default);

    Task PublishPatientMoodUpdatedAsync(
        Guid patientId,
        Guid medicalEventId,
        IReadOnlyList<Guid> recipientDoctorIds,
        IReadOnlyDictionary<string, string> templateData,
        string priority,
        CancellationToken cancellationToken = default);

    Task PublishPatientTriageCompletedAsync(
        Guid patientId,
        Guid medicalEventId,
        IReadOnlyList<Guid> recipientDoctorIds,
        IReadOnlyDictionary<string, string> templateData,
        string priority,
        CancellationToken cancellationToken = default);
}
