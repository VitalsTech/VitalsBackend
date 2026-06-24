namespace NotificationService.Application.Interfaces;

public interface IIntegrationChannelClient
{
    Task<string?> SendSmsAsync(Guid userId, string body, CancellationToken cancellationToken = default);

    Task<string?> SendEmailAsync(Guid userId, string subject, string body, CancellationToken cancellationToken = default);

    Task<string?> SendPushAsync(
        Guid userId,
        string platform,
        string title,
        string body,
        CancellationToken cancellationToken = default);

    Task<string?> SendVoiceAsync(Guid userId, string message, int urgencyLevel, CancellationToken cancellationToken = default);
}
