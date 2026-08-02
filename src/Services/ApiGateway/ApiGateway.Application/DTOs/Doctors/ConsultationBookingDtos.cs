namespace ApiGateway.Application.DTOs.Doctors;

public sealed class BookConsultationRequestDto
{
    public Guid DoctorId { get; set; }
    public Guid SlotId { get; set; }
    public string ConsultationType { get; set; } = "SyncChat";
    public int UrgencyLevel { get; set; } = 3;
    public Guid? TriageSessionId { get; set; }
}

public sealed class BookConsultationResponseDto
{
    public Guid SessionId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid PatientId { get; set; }
    public Guid SlotId { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public bool IsOnline { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    /// <summary>false — консультация создана, но слот не удалось связать с ней; календарь сопоставит по времени.</summary>
    public bool SlotLinked { get; set; }
}

public enum BookConsultationOutcome
{
    Booked,
    SlotUnavailable,
    DoctorNotFound,
    ConsultationFailed
}

public sealed class BookConsultationResult
{
    public BookConsultationOutcome Outcome { get; set; }
    public BookConsultationResponseDto? Booking { get; set; }
    public string? Error { get; set; }
}
