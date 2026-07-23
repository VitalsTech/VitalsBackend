namespace UserService.Application.DTOs.Doctor;

public sealed class DoctorSearchRequest
{
    public string? Query { get; set; }
    public string? Specialization { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; } = 20;
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

public sealed class DoctorSearchResponseDto
{
    public IReadOnlyList<DoctorCardDto> Items { get; set; } = Array.Empty<DoctorCardDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
