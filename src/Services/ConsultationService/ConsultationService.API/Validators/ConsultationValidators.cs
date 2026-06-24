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
