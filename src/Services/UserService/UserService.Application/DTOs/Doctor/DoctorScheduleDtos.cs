namespace UserService.Application.DTOs.Doctor;

public sealed class DoctorScheduleSlotDto
{
    public Guid? Id { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsOnline { get; set; }
}

public sealed class UpsertDoctorScheduleSlotRequest
{
    public Guid? Id { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public bool IsAvailable { get; set; } = true;
    public bool IsOnline { get; set; }
}

public sealed class DoctorScheduleResponseDto
{
    public Guid DoctorId { get; set; }
    public IReadOnlyList<DoctorScheduleSlotDto> Slots { get; set; } = Array.Empty<DoctorScheduleSlotDto>();
}

/// <summary>
/// Слот с данными брони. Только для internal-вызовов: содержит идентификатор пациента,
/// поэтому в публичном расписании не отдаётся.
/// </summary>
public sealed class DoctorScheduleSlotBookingDto
{
    public Guid Id { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsOnline { get; set; }
    public Guid? PatientId { get; set; }
    public Guid? ConsultationSessionId { get; set; }
    public DateTime? BookedAt { get; set; }
}

public sealed class DoctorScheduleBookingResponseDto
{
    public Guid DoctorId { get; set; }
    public IReadOnlyList<DoctorScheduleSlotBookingDto> Slots { get; set; } = Array.Empty<DoctorScheduleSlotBookingDto>();
}

public sealed class AvailableDoctorDto
{
    public Guid DoctorId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public int TodayLoad { get; set; }
    public bool IsSenior { get; set; }
}
