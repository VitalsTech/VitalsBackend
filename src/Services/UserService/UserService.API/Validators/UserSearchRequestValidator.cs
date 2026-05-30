using FluentValidation;
using UserService.Application.DTOs.Common;

namespace UserService.API.Validators
{
    public class UserSearchRequestValidator : AbstractValidator<UserSearchRequest>
    {
        public UserSearchRequestValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(0).WithMessage("Page must be at least 0");

            RuleFor(x => x.PageSize)
                .GreaterThanOrEqualTo(1).WithMessage("PageSize must be at least 1")
                .LessThanOrEqualTo(100).WithMessage("PageSize cannot exceed 100");
        }
    }
}
