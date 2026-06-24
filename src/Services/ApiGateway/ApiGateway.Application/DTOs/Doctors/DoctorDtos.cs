namespace ApiGateway.Application.DTOs.Doctors;

public sealed class DoctorSearchResponseDto
{
    public IReadOnlyList<DoctorCardDto> Items { get; set; } = Array.Empty<DoctorCardDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public sealed class DoctorCardDto
{
    public Guid DoctorId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Specialization { get; set; }
    public double? Rating { get; set; }
    public string? Biography { get; set; }
    public bool IsActive { get; set; }
}

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
