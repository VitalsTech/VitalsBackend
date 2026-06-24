namespace ApiGateway.Application.DTOs.Users;

public sealed class AddProfileRequestDto
{
    public Guid PublicId { get; set; }
    public string ProfileType { get; set; } = string.Empty;
    public object? PatientProfile { get; set; }
    public object? DoctorProfile { get; set; }
    public object? OrganizationProfile { get; set; }
}

public sealed class SwitchProfileRequestDto
{
    public Guid PublicId { get; set; }
    public Guid ProfileId { get; set; }
}

public sealed class CreateUserProfileRequestDto
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? SecondName { get; set; }
    public string Surename { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public string Sex { get; set; } = string.Empty;
    public object? PatientProfile { get; set; }
    public object? DoctorProfile { get; set; }
    public object? OrganizationProfile { get; set; }
}
