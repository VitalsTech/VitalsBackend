namespace ApiGateway.Application.DTOs.Notifications;

public sealed class RegisterPushTokenRequestDto
{
    public string Platform { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}

public sealed class UserPreferenceDto
{
    public string Category { get; set; } = string.Empty;
    public bool PushEnabled { get; set; }
    public bool SmsEnabled { get; set; }
    public bool EmailEnabled { get; set; }
    public bool VoiceEnabled { get; set; }
}
