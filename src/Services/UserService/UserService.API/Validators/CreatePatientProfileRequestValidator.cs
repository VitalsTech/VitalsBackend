using FluentValidation;
using UserService.Application.DTOs.Common;
using UserService.Application.DTOs.Patient;

namespace UserService.API.Validators
{
    public class CreatePatientProfileRequestValidator : AbstractValidator<CreatePatientProfileRequest>
    {
        public CreatePatientProfileRequestValidator()
        {
            RuleFor(x => x.InsuranceNumber)
                .MaximumLength(20).WithMessage("Insurance number cannot exceed 20 characters")
                .When(x => !string.IsNullOrEmpty(x.InsuranceNumber));

            RuleFor(x => x.SNILS)
                .Matches(@"^\d{11}$").WithMessage("SNILS must be 11 digits")
                .When(x => !string.IsNullOrEmpty(x.SNILS));

            RuleFor(x => x.BloodType)
                .Must(bt => bt == null || 
                            bt == "APositive" || bt == "ANegative" || 
                            bt == "BPositive" || bt == "BNegative" || 
                            bt == "ABPositive" || bt == "ABNegative" || 
                            bt == "OPositive" || bt == "ONegative")
                .WithMessage("Invalid blood type");
        }
    }
}