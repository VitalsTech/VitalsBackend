using UserService.Application.DTOs.Doctor;
using UserService.Application.DTOs.Organization;
using UserService.Application.DTOs.Patient;

namespace UserService.Application.DTOs.Common
{
    public class CreateUserWithProfileRequest
    {
        // Общие поля для User
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string? SecondName { get; set; }
        public string Surename { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; }
        public string Sex { get; set; } = string.Empty;

        // Только один из трех (используем новые DTO)
        public CreatePatientProfileRequest? PatientProfile { get; set; }
        public CreateDoctorProfileRequest? DoctorProfile { get; set; }
        public CreateOrganizationProfileRequest? OrganizationProfile { get; set; }
    }

    public class AddProfileToExistingUserRequest
    {
        public Guid UserPublicId { get; set; }
        public string ProfileType { get; set; } = string.Empty; // "Patient", "Doctor", "Organization"

        public CreatePatientProfileRequest? PatientProfile { get; set; }
        public CreateDoctorProfileRequest? DoctorProfile { get; set; }
        public CreateOrganizationProfileRequest? OrganizationProfile { get; set; }
    }

    public class SwitchActiveProfileRequest
    {
        public Guid UserPublicId { get; set; }
        public Guid ProfileId { get; set; }
        public string? ProfileType { get; set; }
    }

    public class ActiveProfileResponse
    {
        public Guid ProfileId { get; set; }
        public string ProfileType { get; set; } = string.Empty;
        public object ProfileData { get; set; } = null!;
    }
}