using ApiGateway.Application.DTOs.Auth;
using FluentValidation;

namespace ApiGateway.API.Validators;

public sealed class RegisterRequestDtoValidator : AbstractValidator<RegisterRequestDto>
{
    public RegisterRequestDtoValidator()
    {
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Surename).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Sex).NotEmpty();
        RuleFor(x => x.BirthDate).LessThan(DateTime.UtcNow.Date);
        RuleFor(x => x)
            .Must(x => x.PatientProfile is not null || x.DoctorProfile is not null || x.OrganizationProfile is not null)
            .WithMessage("At least one profile must be provided.");
    }
}

public sealed class LoginRequestDtoValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestDtoValidator()
    {
        RuleFor(x => x.PhoneNumber).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class RefreshTokenRequestDtoValidator : AbstractValidator<RefreshTokenRequestDto>
{
    public RefreshTokenRequestDtoValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}

public sealed class ChangePasswordRequestDtoValidator : AbstractValidator<ChangePasswordRequestDto>
{
    public ChangePasswordRequestDtoValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).MinimumLength(8).MaximumLength(128);
    }
}

public sealed class ResetPasswordRequestDtoValidator : AbstractValidator<ResetPasswordRequestDto>
{
    public ResetPasswordRequestDtoValidator()
    {
        RuleFor(x => x.PhoneNumber).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().Length(6);
        RuleFor(x => x.NewPassword).MinimumLength(8);
    }
}
