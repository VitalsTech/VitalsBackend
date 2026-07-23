using ApiGateway.Application.Consultations;
using ApiGateway.Application.DTOs.Admin;
using ApiGateway.Application.DTOs.Consultations;
using ApiGateway.Application.DTOs.MedicalRecords;
using ApiGateway.Application.DTOs.Notifications;
using ApiGateway.Application.DTOs.Prescriptions;
using ApiGateway.Application.DTOs.Triage;
using ApiGateway.Application.DTOs.Users;
using FluentValidation;

namespace ApiGateway.API.Validators;

public sealed class UserSearchRequestDtoValidator : AbstractValidator<UserSearchRequestDto>
{
    public UserSearchRequestDtoValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class CreateConsultationRequestDtoValidator : AbstractValidator<CreateConsultationRequestDto>
{
    private static readonly string[] AllowedUrgency = ["Normal", "Urgent", "Emergency"];

    public CreateConsultationRequestDtoValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.DoctorId).NotEmpty();
        RuleFor(x => x.ConsultationType)
            .Must(ConsultationTypeNormalizer.IsAllowed)
            .WithMessage("ConsultationType must be SyncChat, Video, Async, InPerson, or HomeVisit (aliases: chat, video, async/audio, inperson, homevisit; legacy AsyncChat/Audio accepted).");
        RuleFor(x => x.Urgency)
            .Must(u => string.IsNullOrWhiteSpace(u) || AllowedUrgency.Contains(u, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Urgency must be Normal, Urgent, or Emergency.");
        RuleFor(x => x.PrimarySymptom).MaximumLength(2000);
        // 0 = omitted/default from some clients; Create normalizes it to 3.
        RuleFor(x => x.UrgencyLevel).Must(u => u == 0 || (u >= 1 && u <= 5))
            .WithMessage("UrgencyLevel must be between 1 and 5.");
    }
}

public sealed class SendMessageRequestDtoValidator : AbstractValidator<SendMessageRequestDto>
{
    public SendMessageRequestDtoValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.MessageType).NotEmpty();
    }
}

public sealed class CompleteConsultationRequestDtoValidator : AbstractValidator<CompleteConsultationRequestDto>
{
    public CompleteConsultationRequestDtoValidator()
    {
        RuleFor(x => x.Complaints).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.Anamnesis).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.PreliminaryDiagnosisIcd10).NotEmpty().MaximumLength(32);
        RuleFor(x => x.PreliminaryDiagnosisText).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Recommendations).NotEmpty().MaximumLength(4000);
    }
}

public sealed class CreatePrescriptionRequestDtoValidator : AbstractValidator<CreatePrescriptionRequestDto>
{
    public CreatePrescriptionRequestDtoValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.DiagnosisForPrescription).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Medications).NotEmpty();
        RuleForEach(x => x.Medications).ChildRules(m =>
        {
            m.RuleFor(x => x.TradeName).NotEmpty().MaximumLength(200);
            m.RuleFor(x => x.Inn).MaximumLength(200);
            m.RuleFor(x => x.CourseDays).GreaterThan(0).LessThanOrEqualTo(365);
        });
    }
}

public sealed class AppendEventRequestDtoValidator : AbstractValidator<AppendEventRequestDto>
{
    public AppendEventRequestDtoValidator()
    {
        RuleFor(x => x.EventType).NotEmpty().MaximumLength(128);
        RuleFor(x => x.SourceService).NotEmpty().MaximumLength(64);
        RuleFor(x => x.PayloadJson).NotEmpty();
    }
}

public sealed class CreateTriageSessionRequestDtoValidator : AbstractValidator<CreateTriageSessionRequestDto>
{
    public CreateTriageSessionRequestDtoValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.ChiefComplaint).MaximumLength(2000);
    }
}

public sealed class SendTriageMessageRequestDtoValidator : AbstractValidator<SendTriageMessageRequestDto>
{
    public SendTriageMessageRequestDtoValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(4000);
    }
}

public sealed class RegisterPushTokenRequestDtoValidator : AbstractValidator<RegisterPushTokenRequestDto>
{
    public RegisterPushTokenRequestDtoValidator()
    {
        RuleFor(x => x.Platform).NotEmpty().Must(p => p is "ios" or "android" or "web")
            .WithMessage("Platform must be ios, android, or web.");
        RuleFor(x => x.Token).NotEmpty().MaximumLength(512);
    }
}

public sealed class UserPreferenceDtoValidator : AbstractValidator<UserPreferenceDto>
{
    public UserPreferenceDtoValidator()
    {
        RuleFor(x => x.Category).NotEmpty().MaximumLength(64);
    }
}

public sealed class SwitchProfileRequestDtoValidator : AbstractValidator<SwitchProfileRequestDto>
{
    public SwitchProfileRequestDtoValidator()
    {
        RuleFor(x => x.PublicId).NotEmpty();
        RuleFor(x => x.ProfileId).NotEmpty();
    }
}

public sealed class PermissionCheckRequestDtoValidator : AbstractValidator<PermissionCheckRequestDto>
{
    public PermissionCheckRequestDtoValidator()
    {
        RuleFor(x => x.UserPublicId).NotEmpty();
        RuleFor(x => x.Permission).NotEmpty();
    }
}

public sealed class ForgotPasswordRequestDtoValidator : AbstractValidator<Application.DTOs.Auth.ForgotPasswordRequestDto>
{
    public ForgotPasswordRequestDtoValidator()
    {
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Channel).Must(c => c is "sms" or "email").WithMessage("Channel must be sms or email.");
    }
}

public sealed class LogoutRequestDtoValidator : AbstractValidator<Application.DTOs.Auth.LogoutRequestDto>
{
    public LogoutRequestDtoValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
