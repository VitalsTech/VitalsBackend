using FluentValidation;
using UserService.Application.DTOs.Common;
using UserService.Application.DTOs.Organization;

namespace UserService.API.Validators
{
    public class CreateOrganizationProfileRequestValidator : AbstractValidator<CreateOrganizationProfileRequest>
    {
        public CreateOrganizationProfileRequestValidator()
        {
            RuleFor(x => x.LegalName)
                .NotEmpty().WithMessage("Legal name is required")
                .MaximumLength(200).WithMessage("Legal name cannot exceed 200 characters");

            RuleFor(x => x.DisplayName)
                .NotEmpty().WithMessage("Display name is required")
                .MaximumLength(100).WithMessage("Display name cannot exceed 100 characters");

            RuleFor(x => x.INN)
                .NotEmpty().WithMessage("INN is required")
                .Matches(@"^\d{10}$|^\d{12}$").WithMessage("INN must be 10 or 12 digits");

            RuleFor(x => x.OGRN)
                .NotEmpty().WithMessage("OGRN is required")
                .Matches(@"^\d{13}$|^\d{15}$").WithMessage("OGRN must be 13 or 15 digits");

            RuleFor(x => x.Role)
                .NotEmpty().WithMessage("Role is required")
                .Must(role => role == "Clinic" || role == "Laboratory" || role == "Pharmacy")
                .WithMessage("Role must be one of: Clinic, Laboratory, Pharmacy");

            RuleFor(x => x.ContactPhone)
                .Matches(@"^[0-9]{10,15}$").WithMessage("Contact phone must contain 10-15 digits")
                .When(x => !string.IsNullOrEmpty(x.ContactPhone));

            RuleFor(x => x.ContactEmail)
                .EmailAddress().WithMessage("Invalid contact email format")
                .When(x => !string.IsNullOrEmpty(x.ContactEmail));
        }
    }
}