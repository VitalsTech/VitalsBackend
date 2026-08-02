namespace ApiGateway.Application.DTOs.Doctors;

/// <summary>
/// Календарь врача: слоты расписания, обогащённые данными занявшей их консультации.
/// </summary>
public sealed class DoctorCalendarResponseDto
{
    public Guid DoctorId { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public IReadOnlyList<DoctorCalendarSlotDto> Slots { get; set; } = Array.Empty<DoctorCalendarSlotDto>();

    /// <summary>
    /// Консультации врача, которые не попали ни в один слот расписания
    /// (созданы триажем/маршрутизацией вне сетки приёма).
    /// </summary>
    public IReadOnlyList<DoctorCalendarConsultationDto> UnscheduledConsultations { get; set; } =
        Array.Empty<DoctorCalendarConsultationDto>();
}

public sealed class DoctorCalendarSlotDto
{
    public Guid? Id { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public bool IsOnline { get; set; }

    /// <summary>Слот открыт врачом для записи.</summary>
    public bool IsAvailable { get; set; }

    /// <summary>В слот попала консультация.</summary>
    public bool IsBooked { get; set; }

    /// <summary>booked | available | closed.</summary>
    public string Status { get; set; } = string.Empty;

    public DoctorCalendarConsultationDto? Consultation { get; set; }
}

public sealed class DoctorCalendarConsultationDto
{
    /// <summary>null — слот забронирован, но сессия ещё не создана (обрыв записи).</summary>
    public Guid? SessionId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsOpen { get; set; }
    public int UrgencyLevel { get; set; }
    public int ExpectedDurationMinutes { get; set; }
    public DateTime ScheduledAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public int UnreadCount { get; set; }
    public string? VideoRoomId { get; set; }
    public DoctorCalendarPatientDto Patient { get; set; } = new();
    public DoctorCalendarTriageDto? Triage { get; set; }
    public DoctorCalendarAnamnesisDto? Anamnesis { get; set; }
}

public sealed class DoctorCalendarPatientDto
{
    public Guid PatientId { get; set; }
    public string? FullName { get; set; }
    public int? Age { get; set; }
    public string? Sex { get; set; }
}

public sealed class DoctorCalendarTriageDto
{
    public Guid SessionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int UrgencyLevel { get; set; }

    /// <summary>emergency | urgent | routine.</summary>
    public string? Urgency { get; set; }

    public string? UrgencyLabel { get; set; }
    public string? RecommendedSpecialization { get; set; }
    public string? Recommendation { get; set; }
    public bool CanBeRemote { get; set; }

    /// <summary>Первая жалоба пациента своими словами.</summary>
    public string? Complaints { get; set; }

    public IReadOnlyList<string> Symptoms { get; set; } = Array.Empty<string>();
    public IReadOnlyList<DoctorCalendarHypothesisDto> Hypotheses { get; set; } = Array.Empty<DoctorCalendarHypothesisDto>();
    public bool EmergencyWarning { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class DoctorCalendarHypothesisDto
{
    public string Condition { get; set; } = string.Empty;
    public double Probability { get; set; }
}

public sealed class DoctorCalendarAnamnesisDto
{
    public IReadOnlyList<string> ActiveDiagnoses { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> ActiveMedications { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Allergies { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> RecentLabResults { get; set; } = Array.Empty<string>();
    public string? LatestVital { get; set; }

    /// <summary>false — карта пациента пуста либо недоступна.</summary>
    public bool HasData { get; set; }
}
