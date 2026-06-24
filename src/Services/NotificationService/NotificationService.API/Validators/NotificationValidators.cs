using FluentValidation;
using NotificationService.Application.DTOs;

namespace NotificationService.API.Validators;

public sealed class NotificationEventValidator : AbstractValidator<NotificationEventDto>
{
    public NotificationEventValidator()
    {
        RuleFor(x => x.EventId).NotEmpty();
        RuleFor(x => x.EventType).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
