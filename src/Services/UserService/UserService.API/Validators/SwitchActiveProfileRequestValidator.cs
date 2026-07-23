using FluentValidation;
using UserService.Application.DTOs.Common;

namespace UserService.API.Validators
{
    public class SwitchActiveProfileRequestValidator : AbstractValidator<SwitchActiveProfileRequest>
    {
        public SwitchActiveProfileRequestValidator()
        {
            RuleFor(x => x.UserPublicId)
                .NotEmpty().WithMessage("UserPublicId is required");

            // AuthService activates by profileType (Patient/Doctor) without knowing ProfileId yet.
            // Internal switch-profile resolves ProfileId from ProfileType before switching.
            RuleFor(x => x.ProfileId)
                .NotEmpty()
                .When(x => string.IsNullOrWhiteSpace(x.ProfileType))
                .WithMessage("ProfileId is required when ProfileType is not set");

            RuleFor(x => x)
                .Must(x => x.ProfileId != Guid.Empty || !string.IsNullOrWhiteSpace(x.ProfileType))
                .WithMessage("Either ProfileId or ProfileType is required");
        }
    }
}
