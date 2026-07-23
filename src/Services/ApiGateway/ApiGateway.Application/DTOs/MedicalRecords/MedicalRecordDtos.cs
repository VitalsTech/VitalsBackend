namespace ApiGateway.Application.DTOs.MedicalRecords;

public sealed class AppendEventRequestDto
{
    /// <summary>Canonical EventType, e.g. DiagnosisConfirmed, DocumentUploaded.</summary>
    public string EventType { get; set; } = string.Empty;
    public string SourceService { get; set; } = string.Empty;
    /// <summary>JSON payload as a string (gateway parses it into an object for MedicalRecordService).</summary>
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime? OccurredAt { get; set; }
}

public sealed class CreateAccessGrantRequestDto
{
    public Guid GranteeUserId { get; set; }
    public string GranteeRole { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public string? Reason { get; set; }
}

public sealed class AppendEventResponseDto
{
    public Guid EventId { get; set; }
    public long Version { get; set; }
    public DateTime OccurredAt { get; set; }
}

public sealed class PatientHistoryResponseDto
{
    public Guid PatientId { get; set; }
    public long? SnapshotVersion { get; set; }
    public PatientCurrentStateDto CurrentState { get; set; } = new();
    public IReadOnlyList<MedicalEventDto> Events { get; set; } = Array.Empty<MedicalEventDto>();
}

public sealed class MedicalEventDto
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public long Version { get; set; }
    public object? Payload { get; set; }
    public DateTime OccurredAt { get; set; }
    public string SourceService { get; set; } = string.Empty;
}

public sealed class PatientCurrentStateDto
{
    public IReadOnlyList<object> ActiveDiagnoses { get; set; } = Array.Empty<object>();
    public IReadOnlyList<object> ActivePrescriptions { get; set; } = Array.Empty<object>();
    public IReadOnlyList<object> Allergies { get; set; } = Array.Empty<object>();
    public IReadOnlyList<object> Immunizations { get; set; } = Array.Empty<object>();
    public object? LatestVital { get; set; }
    public IReadOnlyList<object> RecentLabResults { get; set; } = Array.Empty<object>();
}
