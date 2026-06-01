using FluentValidation;
using MedicalRecordService.Application.DTOs;

namespace MedicalRecordService.API.Validators;

public sealed class AppendEventRequestValidator : AbstractValidator<AppendEventRequest>
{
    public AppendEventRequestValidator()
    {
        RuleFor(x => x.EventId).NotEmpty();
        RuleFor(x => x.EventType).NotEmpty().MaximumLength(128);
        RuleFor(x => x.SourceService).NotEmpty().MaximumLength(64);
    }
}
