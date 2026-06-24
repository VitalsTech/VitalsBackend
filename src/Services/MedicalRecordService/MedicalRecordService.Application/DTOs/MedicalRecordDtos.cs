using System.Text.Json;

namespace MedicalRecordService.Application.DTOs;

public sealed class AppendEventRequest
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public JsonElement Payload { get; set; }
    public string SourceService { get; set; } = string.Empty;
    public Guid? CorrelationId { get; set; }
}

public sealed class AppendEventResponse
{
    public Guid EventId { get; set; }
    public long Version { get; set; }
    public DateTime OccurredAt { get; set; }
}

public sealed class MedicalEventDto
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public long Version { get; set; }
    public JsonElement Payload { get; set; }
    public DateTime OccurredAt { get; set; }
    public string SourceService { get; set; } = string.Empty;
    public Guid? CorrelationId { get; set; }
    public Guid ActorUserId { get; set; }
    public string? ActorRole { get; set; }
}

public sealed class PatientHistoryResponse
{
    public Guid PatientId { get; set; }
    public long? SnapshotVersion { get; set; }
    public PatientCurrentStateDto CurrentState { get; set; } = new();
    public IReadOnlyList<MedicalEventDto> Events { get; set; } = Array.Empty<MedicalEventDto>();
}

public sealed class PatientCurrentStateDto
{
    public IReadOnlyList<DiagnosisDto> ActiveDiagnoses { get; set; } = Array.Empty<DiagnosisDto>();
    public IReadOnlyList<PrescriptionDto> ActivePrescriptions { get; set; } = Array.Empty<PrescriptionDto>();
    public IReadOnlyList<AllergyDto> Allergies { get; set; } = Array.Empty<AllergyDto>();
    public IReadOnlyList<ImmunizationDto> Immunizations { get; set; } = Array.Empty<ImmunizationDto>();
    public VitalSignDto? LatestVital { get; set; }
    public IReadOnlyList<LabResultDto> RecentLabResults { get; set; } = Array.Empty<LabResultDto>();
}

public sealed class DiagnosisDto
{
    public string Icd10Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime RecordedAt { get; set; }
    public Guid SourceEventId { get; set; }
}

public sealed class PrescriptionDto
{
    public string MedicationName { get; set; } = string.Empty;
    public string? Dosage { get; set; }
    public string? Instructions { get; set; }
    public DateTime PrescribedAt { get; set; }
}

public sealed class AllergyDto
{
    public string Allergen { get; set; } = string.Empty;
    public string? Severity { get; set; }
}

public sealed class ImmunizationDto
{
    public string VaccineName { get; set; } = string.Empty;
    public DateTime AdministeredAt { get; set; }
}

public sealed class VitalSignDto
{
    public string VitalType { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public DateTime RecordedAt { get; set; }
}

public sealed class LabResultDto
{
    public string TestName { get; set; } = string.Empty;
    public string ResultValue { get; set; } = string.Empty;
    public bool IsCritical { get; set; }
    public DateTime ReceivedAt { get; set; }
}

public sealed class CreateAccessGrantRequest
{
    public Guid GranteeId { get; set; }
    public string GranteeType { get; set; } = "Doctor";
    public IReadOnlyList<string> Scopes { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string>? RestrictedCategories { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public sealed class AccessGrantDto
{
    public Guid Id { get; set; }
    public Guid GranteeId { get; set; }
    public string GranteeType { get; set; } = string.Empty;
    public IReadOnlyList<string> Scopes { get; set; } = Array.Empty<string>();
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class ActorContext
{
    public Guid UserId { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
    public bool IsSystemService { get; set; }
    public string? ServiceName { get; set; }
    public string? IpAddress { get; set; }
    public string? SessionId { get; set; }
}

public sealed class PatientAttachmentDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? CdnUrl { get; set; }
    public long SizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }
}
