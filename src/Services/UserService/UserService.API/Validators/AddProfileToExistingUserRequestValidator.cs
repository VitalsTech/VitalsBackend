using FluentValidation;
using UserService.Application.DTOs.Common;

namespace UserService.API.Validators
{
    public class AddProfileToExistingUserRequestValidator : AbstractValidator<AddProfileToExistingUserRequest>
    {
        public AddProfileToExistingUserRequestValidator()
        {
            RuleFor(x => x.UserPublicId)
                .NotEmpty().WithMessage("UserPublicId is required");

            RuleFor(x => x.ProfileType)
                .NotEmpty().WithMessage("ProfileType is required")
                .Must(type => type == "Patient" || type == "Doctor" || type == "Organization")
                .WithMessage("ProfileType must be 'Patient', 'Doctor', or 'Organization'");

            // Валидация профилей (только один)
            RuleFor(x => x)
                .Must(x => (x.PatientProfile != null ? 1 : 0) + 
                           (x.DoctorProfile != null ? 1 : 0) + 
                           (x.OrganizationProfile != null ? 1 : 0) == 1)
                .WithMessage("Exactly one profile must be provided");

            When(x => x.PatientProfile != null, () => {
                RuleFor(x => x.PatientProfile).SetValidator(new CreatePatientProfileRequestValidator());
            });
            When(x => x.DoctorProfile != null, () => {
                RuleFor(x => x.DoctorProfile).SetValidator(new CreateDoctorProfileRequestValidator());
            });
            When(x => x.OrganizationProfile != null, () => {
                RuleFor(x => x.OrganizationProfile).SetValidator(new CreateOrganizationProfileRequestValidator());
            });
        }
    }
}