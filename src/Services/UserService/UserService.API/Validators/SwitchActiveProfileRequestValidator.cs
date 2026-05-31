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

            RuleFor(x => x.ProfileId)
                .NotEmpty().WithMessage("ProfileId is required");
        }
    }
}