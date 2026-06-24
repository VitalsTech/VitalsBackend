using MedicalRecordService.Domain.Entities;

namespace MedicalRecordService.Domain.Interfaces;

public interface IMedicalEventRepository
{
    Task<MedicalEvent?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<long> GetLatestVersionAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MedicalEvent>> GetEventsAfterVersionAsync(Guid patientId, long afterVersion, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MedicalEvent>> GetEventsAsync(Guid patientId, DateTime? from, DateTime? to, IReadOnlyList<string>? eventTypes, CancellationToken cancellationToken = default);
    Task AddAsync(MedicalEvent medicalEvent, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IPatientSnapshotRepository
{
    Task<PatientSnapshot?> GetLatestAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task AddAsync(PatientSnapshot snapshot, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IProjectionRepository
{
    Task<PatientStateProjections> GetCurrentStateAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task ApplyDiagnosisAsync(PatientDiagnosisProjection item, CancellationToken cancellationToken = default);
    Task SupersedeDiagnosisAsync(Guid patientId, Guid supersededByEventId, string? icd10Code, CancellationToken cancellationToken = default);
    Task ApplyPrescriptionAsync(PatientPrescriptionProjection item, CancellationToken cancellationToken = default);
    Task RevokePrescriptionAsync(Guid patientId, Guid sourceEventId, CancellationToken cancellationToken = default);
    Task ApplyAllergyAsync(PatientAllergyProjection item, CancellationToken cancellationToken = default);
    Task ApplyImmunizationAsync(PatientImmunizationProjection item, CancellationToken cancellationToken = default);
    Task ApplyVitalSignAsync(PatientVitalSignProjection item, CancellationToken cancellationToken = default);
    Task ApplyLabResultAsync(PatientLabResultProjection item, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public sealed class PatientStateProjections
{
    public IReadOnlyList<PatientDiagnosisProjection> Diagnoses { get; init; } = Array.Empty<PatientDiagnosisProjection>();
    public IReadOnlyList<PatientPrescriptionProjection> Prescriptions { get; init; } = Array.Empty<PatientPrescriptionProjection>();
    public IReadOnlyList<PatientAllergyProjection> Allergies { get; init; } = Array.Empty<PatientAllergyProjection>();
    public IReadOnlyList<PatientImmunizationProjection> Immunizations { get; init; } = Array.Empty<PatientImmunizationProjection>();
    public PatientVitalSignProjection? LatestVital { get; init; }
    public IReadOnlyList<PatientLabResultProjection> RecentLabResults { get; init; } = Array.Empty<PatientLabResultProjection>();
}

public interface IAccessGrantRepository
{
    Task<IReadOnlyList<AccessGrant>> GetActiveGrantsAsync(Guid patientId, Guid granteeId, CancellationToken cancellationToken = default);
    Task<AccessGrant?> GetByIdAsync(Guid grantId, CancellationToken cancellationToken = default);
    Task AddAsync(AccessGrant grant, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IAuditLogRepository
{
    Task AddAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IPatientAttachmentRepository
{
    Task AddAsync(PatientAttachment attachment, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PatientAttachment>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
