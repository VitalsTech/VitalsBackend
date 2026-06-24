namespace UserService.Application.DTOs.Doctor;

public sealed class DoctorScheduleSlotDto
{
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public bool IsAvailable { get; set; }
}

public sealed class DoctorScheduleResponseDto
{
    public Guid DoctorId { get; set; }
    public IReadOnlyList<DoctorScheduleSlotDto> Slots { get; set; } = Array.Empty<DoctorScheduleSlotDto>();
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
