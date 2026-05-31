using FluentValidation;
using UserService.Application.DTOs.Common;

namespace UserService.API.Validators
{
    public class CreateUserWithProfileRequestValidator : AbstractValidator<CreateUserWithProfileRequest>
    {
        public CreateUserWithProfileRequestValidator()
        {
            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required")
                .Matches(@"^[0-9]{10,15}$").WithMessage("Phone number must contain 10-15 digits");

            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Invalid email format")
                .When(x => !string.IsNullOrEmpty(x.Email));

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required")
                .MaximumLength(100).WithMessage("First name cannot exceed 100 characters");

            RuleFor(x => x.Surename)
                .NotEmpty().WithMessage("Surename is required")
                .MaximumLength(100).WithMessage("Surename cannot exceed 100 characters");

            RuleFor(x => x.BirthDate)
                .NotEmpty().WithMessage("Birth date is required")
                .Must(date => date <= DateTime.UtcNow).WithMessage("Birth date cannot be in the future")
                .Must(date => date >= DateTime.UtcNow.AddYears(-120)).WithMessage("Birth date is too far in the past");

            RuleFor(x => x.Sex)
                .NotEmpty().WithMessage("Sex is required")
                .Must(sex => sex == "Male" || sex == "Female").WithMessage("Sex must be 'Male' or 'Female'");

            RuleFor(x => x)
                .Must(x => (x.PatientProfile != null ? 1 : 0) + 
                           (x.DoctorProfile != null ? 1 : 0) + 
                           (x.OrganizationProfile != null ? 1 : 0) == 1)
                .WithMessage("Exactly one profile (Patient, Doctor, or Organization) must be provided");

            When(x => x.PatientProfile != null, () => {
                RuleFor(x => x.PatientProfile!).SetValidator(new CreatePatientProfileRequestValidator());
            });
            When(x => x.DoctorProfile != null, () => {
                RuleFor(x => x.DoctorProfile!).SetValidator(new CreateDoctorProfileRequestValidator());
            });
            When(x => x.OrganizationProfile != null, () => {
                RuleFor(x => x.OrganizationProfile!).SetValidator(new CreateOrganizationProfileRequestValidator());
            });
        }
    }
}