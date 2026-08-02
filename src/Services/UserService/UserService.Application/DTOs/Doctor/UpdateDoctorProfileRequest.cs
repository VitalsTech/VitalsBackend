namespace UserService.Application.DTOs.Doctor;

public sealed class UpdateDoctorProfileRequest
{
    public string? Specialization { get; set; }
    public string? Biography { get; set; }
    public string? AcademicDegree { get; set; }
}
