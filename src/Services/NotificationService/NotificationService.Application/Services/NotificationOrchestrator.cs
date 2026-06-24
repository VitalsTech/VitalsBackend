using NotificationService.Application.DTOs;
using NotificationService.Application.Interfaces;
using NotificationService.Application.Options;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NotificationService.Application.Services;

public sealed class NotificationOrchestrator : INotificationOrchestrator
{
    private readonly INotificationRepository _repo;
    private readonly IEventChannelRouter _router;
    private readonly ITemplateRenderer _renderer;
    private readonly IUserPreferenceService _preferences;
    private readonly IEnumerable<IChannelDispatcher> _dispatchers;
    private readonly NotificationOptions _options;
    private readonly ILogger<NotificationOrchestrator> _logger;

    public NotificationOrchestrator(
        INotificationRepository repo,
        IEventChannelRouter router,
        ITemplateRenderer renderer,
        IUserPreferenceService preferences,
        IEnumerable<IChannelDispatcher> dispatchers,
        IOptions<NotificationOptions> options,
        ILogger<NotificationOrchestrator> logger)
    {
        _repo = repo;
        _router = router;
        _renderer = renderer;
        _preferences = preferences;
        _dispatchers = dispatchers;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ProcessEventResponse> ProcessEventAsync(
        NotificationEventDto notificationEvent,
        CancellationToken cancellationToken = default)
    {
        if (await _repo.IsEventProcessedAsync(notificationEvent.EventId, cancellationToken))
        {
            return new ProcessEventResponse
            {
                SourceEventId = notificationEvent.EventId,
                WasDuplicate = true,
                Deliveries = []
            };
        }

        var routes = _router.ResolveRoutes(notificationEvent);
        var deliveries = new List<NotificationDeliveryResponse>();
        var priority = ParsePriority(notificationEvent.Priority);

        foreach (var (templateKey, channel) in routes)
        {
            var targetUserId = ResolveTargetUser(notificationEvent, templateKey);
            if (!await _preferences.IsChannelEnabledAsync(targetUserId, notificationEvent.Category, channel, cancellationToken))
            {
                deliveries.Add(await SaveSkippedAsync(notificationEvent, targetUserId, channel, templateKey, priority, cancellationToken));
                continue;
            }

            var template = await _repo.GetTemplateAsync(templateKey, channel, "ru", cancellationToken);
            if (template is null)
            {
                _logger.LogWarning("Template missing: {Key}/{Channel}", templateKey, channel);
                continue;
            }

            var rendered = _renderer.Render(template, notificationEvent.TemplateData);
            var delivery = await DispatchAsync(notificationEvent, targetUserId, channel, templateKey, rendered, priority, cancellationToken);
            deliveries.Add(delivery);
        }

        await _repo.MarkEventProcessedAsync(notificationEvent.EventId, notificationEvent.EventType, cancellationToken);

        return new ProcessEventResponse
        {
            SourceEventId = notificationEvent.EventId,
            WasDuplicate = false,
            Deliveries = deliveries
        };
    }

    public async Task<NotificationDeliveryResponse?> GetDeliveryStatusAsync(Guid deliveryId, CancellationToken cancellationToken = default)
    {
        var log = await _repo.GetDeliveryByIdAsync(deliveryId, cancellationToken);
        return log is null ? null : MapDelivery(log);
    }

    public async Task RetryDeliveryAsync(Guid deliveryId, CancellationToken cancellationToken = default)
    {
        var log = await _repo.GetDeliveryByIdAsync(deliveryId, cancellationToken)
            ?? throw new KeyNotFoundException($"Delivery {deliveryId} not found.");

        if (log.Status == DeliveryStatus.Delivered.ToString())
            return;

        var template = await _repo.GetTemplateAsync(log.EventType, log.Channel, "ru", cancellationToken);
        if (template is null)
            throw new InvalidOperationException("Template not found for retry.");

        var rendered = new RenderedMessage
        {
            Subject = log.Subject,
            Body = log.Body,
            TemplateVersion = log.TemplateVersion
        };

        var dispatcher = _dispatchers.FirstOrDefault(d => d.Channel.Equals(log.Channel, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"No dispatcher for {log.Channel}");

        log.AttemptCount += 1;
        var priority = ParsePriority(log.Priority);
        var maxAttempts = priority == NotificationPriority.Critical
            ? _options.CriticalMaxRetryAttempts
            : _options.MaxRetryAttempts;

        var result = await dispatcher.DispatchAsync(log.UserId, rendered, priority, cancellationToken);
        if (result.Success)
        {
            log.Status = DeliveryStatus.Delivered.ToString();
            log.DeliveredAt = DateTime.UtcNow;
            log.ErrorMessage = null;
            log.NextRetryAt = null;
        }
        else if (log.AttemptCount >= maxAttempts)
        {
            await MoveToDeadLetterAsync(log, result.ErrorMessage, cancellationToken);
        }
        else
        {
            log.Status = DeliveryStatus.Failed.ToString();
            log.ErrorMessage = result.ErrorMessage;
            log.NextRetryAt = CalculateNextRetry(log.AttemptCount, priority);
        }

        await _repo.SaveDeliveryLogAsync(log, cancellationToken);
    }

    private async Task MoveToDeadLetterAsync(
        NotificationDeliveryLog log,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        log.Status = "DeadLettered";
        log.ErrorMessage = errorMessage;
        log.NextRetryAt = null;

        await _repo.SaveDeadLetterAsync(new DeadLetterNotification
        {
            Id = Guid.NewGuid(),
            OriginalDeliveryId = log.Id,
            UserId = log.UserId,
            SourceEventId = log.SourceEventId,
            EventType = log.EventType,
            Channel = log.Channel,
            Subject = log.Subject,
            Body = log.Body,
            ErrorMessage = errorMessage,
            AttemptCount = log.AttemptCount,
            Priority = log.Priority,
            MovedAt = DateTime.UtcNow
        }, cancellationToken);

        _logger.LogWarning(
            "Delivery {DeliveryId} moved to DLQ after {Attempts} attempts ({Channel}/{EventType})",
            log.Id,
            log.AttemptCount,
            log.Channel,
            log.EventType);
    }

    private async Task<NotificationDeliveryResponse> DispatchAsync(
        NotificationEventDto notificationEvent,
        Guid userId,
        string channel,
        string templateKey,
        RenderedMessage rendered,
        NotificationPriority priority,
        CancellationToken cancellationToken)
    {
        var dispatcher = _dispatchers.FirstOrDefault(d => d.Channel.Equals(channel, StringComparison.OrdinalIgnoreCase));
        var log = new NotificationDeliveryLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SourceEventId = notificationEvent.EventId,
            EventType = templateKey,
            Channel = channel,
            Subject = rendered.Subject,
            Body = rendered.Body,
            Status = DeliveryStatus.Pending.ToString(),
            AttemptCount = 1,
            TemplateVersion = rendered.TemplateVersion,
            Priority = priority.ToString(),
            CreatedAt = DateTime.UtcNow
        };

        if (dispatcher is null)
        {
            log.Status = DeliveryStatus.Failed.ToString();
            log.ErrorMessage = "Dispatcher not registered";
            await _repo.SaveDeliveryLogAsync(log, cancellationToken);
            return MapDelivery(log);
        }

        var result = await dispatcher.DispatchAsync(userId, rendered, priority, cancellationToken);
        if (result.Success)
        {
            log.Status = DeliveryStatus.Delivered.ToString();
            log.DeliveredAt = DateTime.UtcNow;
        }
        else
        {
            log.Status = DeliveryStatus.Failed.ToString();
            log.ErrorMessage = result.ErrorMessage;
            log.NextRetryAt = CalculateNextRetry(1, priority);
        }

        await _repo.SaveDeliveryLogAsync(log, cancellationToken);
        return MapDelivery(log);
    }

    private async Task<NotificationDeliveryResponse> SaveSkippedAsync(
        NotificationEventDto notificationEvent,
        Guid userId,
        string channel,
        string templateKey,
        NotificationPriority priority,
        CancellationToken cancellationToken)
    {
        var log = new NotificationDeliveryLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SourceEventId = notificationEvent.EventId,
            EventType = templateKey,
            Channel = channel,
            Body = "skipped by user preferences",
            Status = DeliveryStatus.SkippedByPreferences.ToString(),
            AttemptCount = 0,
            Priority = priority.ToString(),
            CreatedAt = DateTime.UtcNow
        };
        await _repo.SaveDeliveryLogAsync(log, cancellationToken);
        return MapDelivery(log);
    }

    private static Guid ResolveTargetUser(NotificationEventDto e, string templateKey)
    {
        if (templateKey.Contains(".doctor.", StringComparison.OrdinalIgnoreCase) && e.SecondaryUserId.HasValue)
            return e.SecondaryUserId.Value;

        return e.UserId;
    }

    private DateTime? CalculateNextRetry(int attempt, NotificationPriority priority)
    {
        var max = priority == NotificationPriority.Critical ? _options.CriticalMaxRetryAttempts : _options.MaxRetryAttempts;
        if (attempt >= max)
            return null;

        var delay = priority == NotificationPriority.Critical
            ? TimeSpan.FromSeconds(30)
            : attempt switch
            {
                1 => TimeSpan.FromSeconds(5),
                2 => TimeSpan.FromSeconds(30),
                3 => TimeSpan.FromMinutes(5),
                4 => TimeSpan.FromMinutes(30),
                _ => TimeSpan.FromHours(2)
            };

        return DateTime.UtcNow.Add(delay);
    }

    private static NotificationPriority ParsePriority(string priority) =>
        Enum.TryParse<NotificationPriority>(priority, true, out var parsed) ? parsed : NotificationPriority.Medium;

    private static NotificationDeliveryResponse MapDelivery(NotificationDeliveryLog log) => new()
    {
        DeliveryId = log.Id,
        UserId = log.UserId,
        EventType = log.EventType,
        Channel = log.Channel,
        Status = log.Status,
        AttemptCount = log.AttemptCount,
        CreatedAt = log.CreatedAt,
        DeliveredAt = log.DeliveredAt
    };
}
