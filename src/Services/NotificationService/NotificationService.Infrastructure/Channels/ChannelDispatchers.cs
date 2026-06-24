using NotificationService.Application.DTOs;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace NotificationService.Infrastructure.Channels;

public sealed class PushChannelDispatcher : IChannelDispatcher
{
    private readonly INotificationRepository _repo;
    private readonly ILogger<PushChannelDispatcher> _logger;

    public PushChannelDispatcher(INotificationRepository repo, ILogger<PushChannelDispatcher> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public string Channel => "Push";

    public async Task<ChannelDispatchResult> DispatchAsync(
        Guid userId,
        RenderedMessage message,
        NotificationPriority priority,
        CancellationToken cancellationToken = default)
    {
        var tokens = await _repo.GetActivePushTokensAsync(userId, cancellationToken);
        if (tokens.Count == 0)
        {
            return new ChannelDispatchResult
            {
                Success = false,
                ErrorMessage = "No active push tokens"
            };
        }

        foreach (var token in tokens)
        {
            _logger.LogInformation(
                "Push stub -> user={UserId} platform={Platform} priority={Priority} title={Title}",
                userId,
                token.Platform,
                priority,
                message.Subject);
        }

        return new ChannelDispatchResult
        {
            Success = true,
            ProviderMessageId = $"push-{Guid.NewGuid():N}"
        };
    }
}

public sealed class SmsChannelDispatcher : IChannelDispatcher
{
    private readonly ILogger<SmsChannelDispatcher> _logger;

    public SmsChannelDispatcher(ILogger<SmsChannelDispatcher> logger) => _logger = logger;

    public string Channel => "Sms";

    public Task<ChannelDispatchResult> DispatchAsync(
        Guid userId,
        RenderedMessage message,
        NotificationPriority priority,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SMS stub via Integration Service -> user={UserId}: {Body}", userId, message.Body);
        return Task.FromResult(new ChannelDispatchResult
        {
            Success = true,
            ProviderMessageId = $"sms-{Guid.NewGuid():N}"
        });
    }
}

public sealed class EmailChannelDispatcher : IChannelDispatcher
{
    private readonly ILogger<EmailChannelDispatcher> _logger;

    public EmailChannelDispatcher(ILogger<EmailChannelDispatcher> logger) => _logger = logger;

    public string Channel => "Email";

    public Task<ChannelDispatchResult> DispatchAsync(
        Guid userId,
        RenderedMessage message,
        NotificationPriority priority,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Email stub via Integration Service -> user={UserId} subject={Subject}", userId, message.Subject);
        return Task.FromResult(new ChannelDispatchResult
        {
            Success = true,
            ProviderMessageId = $"email-{Guid.NewGuid():N}"
        });
    }
}

public sealed class VoiceChannelDispatcher : IChannelDispatcher
{
    private readonly ILogger<VoiceChannelDispatcher> _logger;

    public VoiceChannelDispatcher(ILogger<VoiceChannelDispatcher> logger) => _logger = logger;

    public string Channel => "Voice";

    public Task<ChannelDispatchResult> DispatchAsync(
        Guid userId,
        RenderedMessage message,
        NotificationPriority priority,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Voice call stub -> user={UserId}: {Body}", userId, message.Body);
        return Task.FromResult(new ChannelDispatchResult
        {
            Success = true,
            ProviderMessageId = $"voice-{Guid.NewGuid():N}"
        });
    }
}
