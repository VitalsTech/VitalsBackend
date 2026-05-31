using FluentValidation;
using UserService.Application.DTOs.Common;

namespace UserService.API.Validators
{
    public class PermissionCheckRequestValidator : AbstractValidator<PermissionCheckRequest>
    {
        public PermissionCheckRequestValidator()
        {
            RuleFor(x => x.UserPublicId)
                .NotEmpty().WithMessage("UserPublicId is required");

            RuleFor(x => x.Permission)
                .NotEmpty().WithMessage("Permission is required");
        }
    }
}