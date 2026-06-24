using AITriageService.Application.DTOs;
using FluentValidation;

namespace AITriageService.API.Validators;

public sealed class CreateTriageSessionRequestValidator : AbstractValidator<CreateTriageSessionRequest>
{
    public CreateTriageSessionRequestValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
    }
}

public sealed class SendTriageMessageRequestValidator : AbstractValidator<SendTriageMessageRequest>
{
    public SendTriageMessageRequestValidator()
    {
        RuleFor(x => x.Message).NotEmpty().MaximumLength(4000);
    }
}
