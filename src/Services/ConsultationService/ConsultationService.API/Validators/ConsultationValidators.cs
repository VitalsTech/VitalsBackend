using FluentValidation;
using ConsultationService.Application.DTOs;

namespace ConsultationService.API.Validators;

public sealed class CreateConsultationRequestValidator : AbstractValidator<CreateConsultationRequest>
{
    public CreateConsultationRequestValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.DoctorId).NotEmpty();
        RuleFor(x => x.UrgencyLevel).InclusiveBetween(1, 5);
    }
}

public sealed class SendMessageRequestValidator : AbstractValidator<SendMessageRequest>
{
    public SendMessageRequestValidator()
    {
        RuleFor(x => x.Content).NotEmpty().When(x => x.MessageType == "Text");
        RuleFor(x => x.MessageType).NotEmpty();
    }
}

public sealed class CompleteConsultationRequestValidator : AbstractValidator<CompleteConsultationRequest>
{
    public CompleteConsultationRequestValidator()
    {
        RuleFor(x => x.Complaints).NotEmpty();
        RuleFor(x => x.PreliminaryDiagnosisIcd10).NotEmpty();
        RuleFor(x => x.Recommendations).NotEmpty();
    }
}

public sealed class SubmitRatingRequestValidator : AbstractValidator<SubmitRatingRequest>
{
    public SubmitRatingRequestValidator()
    {
        RuleFor(x => x.Role).NotEmpty();
        RuleFor(x => x.Score).InclusiveBetween(1, 5);
    }
}

public sealed class AddDiagnosisRequestValidator : AbstractValidator<AddDiagnosisRequest>
{
    public AddDiagnosisRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Icd10) || !string.IsNullOrWhiteSpace(x.Text))
            .WithMessage("Icd10 or Text is required.");
        RuleFor(x => x.Icd10).MaximumLength(32);
        RuleFor(x => x.Text).MaximumLength(500);
    }
}

public sealed class AddPrescriptionsRequestValidator : AbstractValidator<AddPrescriptionsRequest>
{
    public AddPrescriptionsRequestValidator()
    {
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).NotEmpty().MaximumLength(500);
    }
}

public sealed class IssueCertificateRequestValidator : AbstractValidator<IssueCertificateRequest>
{
    private static readonly string[] AllowedTypes = ["HealthStatus", "StudyExcuse", "WorkExcuse", "Other"];

    public IssueCertificateRequestValidator()
    {
        RuleFor(x => x.Type)
            .Must(t => string.IsNullOrWhiteSpace(t) || AllowedTypes.Contains(t, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Type must be HealthStatus, StudyExcuse, WorkExcuse, or Other.");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(4000);
    }
}
