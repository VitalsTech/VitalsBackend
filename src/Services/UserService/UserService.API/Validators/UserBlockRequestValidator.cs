using FluentValidation;
using UserService.Application.DTOs.Common;

namespace UserService.API.Validators
{
    public class UserBlockRequestValidator : AbstractValidator<UserBlockRequest>
    {
        public UserBlockRequestValidator()
        {
            RuleFor(x => x.BlockedUntil)
                .Must(blockedUntil => !blockedUntil.HasValue || blockedUntil.Value > DateTime.UtcNow)
                .WithMessage("BlockedUntil must be in the future")
                .When(x => x.BlockedUntil.HasValue);
        }
    }
}
