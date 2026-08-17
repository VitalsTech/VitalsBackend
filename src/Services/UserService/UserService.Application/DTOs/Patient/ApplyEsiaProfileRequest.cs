using UserService.Application.DTOs.Common;

namespace UserService.Application.DTOs.Patient;

public sealed class ApplyEsiaProfileRequest
{
    public string? FirstName { get; set; }
    public string? SecondName { get; set; }
    public string? Surename { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? Sex { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? SNILS { get; set; }
    public string? InsuranceNumber { get; set; }
    public AddressDto? ResidenceAddress { get; set; }
    public AddressDto? RegistrationAddress { get; set; }
}
