using NotificationService.Application.DTOs;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace NotificationService.Infrastructure.Channels;

public sealed class PushChannelDispatcher : IChannelDispatcher
{
    private readonly INotificationRepository _repo;
    private readonly IIntegrationChannelClient _integration;
    private readonly ILogger<PushChannelDispatcher> _logger;

    public PushChannelDispatcher(
        INotificationRepository repo,
        IIntegrationChannelClient integration,
        ILogger<PushChannelDispatcher> logger)
    {
        _repo = repo;
        _integration = integration;
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

        string? lastReference = null;
        foreach (var token in tokens)
        {
            var reference = await _integration.SendPushAsync(
                userId,
                token.Platform,
                message.Subject ?? string.Empty,
                message.Body,
                cancellationToken).ConfigureAwait(false);

            if (reference is not null)
            {
                lastReference = reference;
                continue;
            }

            _logger.LogInformation(
                "Push stub -> user={UserId} platform={Platform} priority={Priority} title={Title}",
                userId,
                token.Platform,
                priority,
                message.Subject);
            lastReference ??= $"push-{Guid.NewGuid():N}";
        }

        return new ChannelDispatchResult
        {
            Success = true,
            ProviderMessageId = lastReference
        };
    }
}

public sealed class SmsChannelDispatcher : IChannelDispatcher
{
    private readonly IIntegrationChannelClient _integration;
    private readonly ILogger<SmsChannelDispatcher> _logger;

    public SmsChannelDispatcher(IIntegrationChannelClient integration, ILogger<SmsChannelDispatcher> logger)
    {
        _integration = integration;
        _logger = logger;
    }

    public string Channel => "Sms";

    public async Task<ChannelDispatchResult> DispatchAsync(
        Guid userId,
        RenderedMessage message,
        NotificationPriority priority,
        CancellationToken cancellationToken = default)
    {
        var reference = await _integration.SendSmsAsync(userId, message.Body, cancellationToken).ConfigureAwait(false);
        if (reference is null)
        {
            _logger.LogInformation("SMS stub via Integration Service -> user={UserId}: {Body}", userId, message.Body);
            reference = $"sms-{Guid.NewGuid():N}";
        }

        return new ChannelDispatchResult
        {
            Success = true,
            ProviderMessageId = reference
        };
    }
}

public sealed class EmailChannelDispatcher : IChannelDispatcher
{
    private readonly IIntegrationChannelClient _integration;
    private readonly ILogger<EmailChannelDispatcher> _logger;

    public EmailChannelDispatcher(IIntegrationChannelClient integration, ILogger<EmailChannelDispatcher> logger)
    {
        _integration = integration;
        _logger = logger;
    }

    public string Channel => "Email";

    public async Task<ChannelDispatchResult> DispatchAsync(
        Guid userId,
        RenderedMessage message,
        NotificationPriority priority,
        CancellationToken cancellationToken = default)
    {
        var reference = await _integration.SendEmailAsync(
            userId,
            message.Subject ?? string.Empty,
            message.Body,
            cancellationToken).ConfigureAwait(false);

        if (reference is null)
        {
            _logger.LogInformation(
                "Email stub via Integration Service -> user={UserId} subject={Subject}",
                userId,
                message.Subject);
            reference = $"email-{Guid.NewGuid():N}";
        }

        return new ChannelDispatchResult
        {
            Success = true,
            ProviderMessageId = reference
        };
    }
}

public sealed class VoiceChannelDispatcher : IChannelDispatcher
{
    private readonly IIntegrationChannelClient _integration;
    private readonly ILogger<VoiceChannelDispatcher> _logger;

    public VoiceChannelDispatcher(IIntegrationChannelClient integration, ILogger<VoiceChannelDispatcher> logger)
    {
        _integration = integration;
        _logger = logger;
    }

    public string Channel => "Voice";

    public async Task<ChannelDispatchResult> DispatchAsync(
        Guid userId,
        RenderedMessage message,
        NotificationPriority priority,
        CancellationToken cancellationToken = default)
    {
        var urgency = priority == NotificationPriority.Critical ? 3 : 1;
        var reference = await _integration.SendVoiceAsync(userId, message.Body, urgency, cancellationToken).ConfigureAwait(false);
        if (reference is null)
        {
            _logger.LogWarning("Voice call stub -> user={UserId}: {Body}", userId, message.Body);
            reference = $"voice-{Guid.NewGuid():N}";
        }

        return new ChannelDispatchResult
        {
            Success = true,
            ProviderMessageId = reference
        };
    }
}
