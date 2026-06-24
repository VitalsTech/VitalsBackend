using FluentValidation;
using PrescriptionService.Application.DTOs;

namespace PrescriptionService.API.Validators;

public sealed class CreatePrescriptionRequestValidator : AbstractValidator<CreatePrescriptionRequest>
{
    public CreatePrescriptionRequestValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.DiagnosisForPrescription).NotEmpty();
        RuleFor(x => x.Medications).NotEmpty();
        RuleForEach(x => x.Medications).SetValidator(new MedicationItemValidator());
    }
}

public sealed class MedicationItemValidator : AbstractValidator<MedicationItemDto>
{
    public MedicationItemValidator()
    {
        RuleFor(x => x.TradeName).NotEmpty();
        RuleFor(x => x.Inn).NotEmpty();
        RuleFor(x => x.AtcCode).NotEmpty();
        RuleFor(x => x.CourseDays).GreaterThan(0);
    }
}
