using FluentValidation;
using UserService.Application.DTOs.Common;
using UserService.Application.DTOs.Doctor;
using UserService.Domain.Entities;

namespace UserService.API.Validators
{
    public class CreateDoctorProfileRequestValidator : AbstractValidator<CreateDoctorProfileRequest>
    {
        public CreateDoctorProfileRequestValidator()
        {
            RuleFor(x => x.Specialization)
                .NotEmpty().WithMessage("Specialization is required")
                .MaximumLength(100).WithMessage("Specialization cannot exceed 100 characters");

            RuleFor(x => x.DiplomaNumber)
                .NotEmpty().WithMessage("Diploma number is required")
                .MaximumLength(50).WithMessage("Diploma number cannot exceed 50 characters");

            RuleFor(x => x.CertificateNumber)
                .NotEmpty().WithMessage("Certificate number is required")
                .MaximumLength(50).WithMessage("Certificate number cannot exceed 50 characters");

            RuleFor(x => x.CertificateExpiryDate)
                .NotEmpty().WithMessage("Certificate expiry date is required")
                .Must(date => date > DateTime.UtcNow).WithMessage("Certificate expiry date must be in the future");

            RuleFor(x => x.Category)
                .NotEmpty().WithMessage("Category is required")
                .Must(category => category == "None" || category == "Second" || category == "First" || category == "Highest")
                .WithMessage("Category must be one of: None, Second, First, Highest");

            RuleFor(x => x.AcademicDegree)
                .MaximumLength(200).WithMessage("Academic degree cannot exceed 200 characters");

            RuleFor(x => x.Biography)
                .MaximumLength(2000).WithMessage("Biography cannot exceed 2000 characters");
        }
    }
}