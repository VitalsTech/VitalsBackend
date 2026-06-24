using FluentValidation;
using RoutingService.Application.DTOs;

namespace RoutingService.API.Validators;

public sealed class TriageCompletedEventValidator : AbstractValidator<TriageCompletedEventDto>
{
    public TriageCompletedEventValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.UrgencyLevel).InclusiveBetween(1, 5);
    }
}
