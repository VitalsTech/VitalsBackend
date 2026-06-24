using NotificationService.Application.DTOs;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;

namespace NotificationService.Application.Interfaces;

public interface INotificationRepository
{
    Task<bool> IsEventProcessedAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task MarkEventProcessedAsync(Guid eventId, string eventType, CancellationToken cancellationToken = default);
    Task<NotificationTemplate?> GetTemplateAsync(string templateKey, string channel, string language, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationTemplate>> GetAllTemplatesAsync(CancellationToken cancellationToken = default);
    Task SaveTemplateAsync(NotificationTemplate template, CancellationToken cancellationToken = default);
    Task SaveDeliveryLogAsync(NotificationDeliveryLog log, CancellationToken cancellationToken = default);
    Task<NotificationDeliveryLog?> GetDeliveryByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationDeliveryLog>> GetUserHistoryAsync(Guid userId, int limit, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationDeliveryLog>> GetPendingRetriesAsync(DateTime utcNow, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DevicePushToken>> GetActivePushTokensAsync(Guid userId, CancellationToken cancellationToken = default);
    Task SavePushTokenAsync(DevicePushToken token, CancellationToken cancellationToken = default);
    Task<UserNotificationPreference?> GetPreferenceAsync(Guid userId, string category, CancellationToken cancellationToken = default);
    Task SavePreferenceAsync(UserNotificationPreference preference, CancellationToken cancellationToken = default);
    Task<NotificationStatsResponse> GetStatsAsync(DateTime since, CancellationToken cancellationToken = default);
    Task CleanupOldProcessedEventsAsync(DateTime olderThan, CancellationToken cancellationToken = default);
    Task SaveDeadLetterAsync(DeadLetterNotification entry, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DeadLetterNotification>> GetDeadLettersAsync(int limit, CancellationToken cancellationToken = default);
}

public interface ITemplateRenderer
{
    RenderedMessage Render(NotificationTemplate template, IReadOnlyDictionary<string, string> data);
}

public interface IEventChannelRouter
{
    IReadOnlyList<(string TemplateKey, string Channel)> ResolveRoutes(NotificationEventDto notificationEvent);
}

public interface IUserPreferenceService
{
    Task<bool> IsChannelEnabledAsync(Guid userId, string category, string channel, CancellationToken cancellationToken = default);
}

public interface IChannelDispatcher
{
    string Channel { get; }
    Task<ChannelDispatchResult> DispatchAsync(Guid userId, RenderedMessage message, NotificationPriority priority, CancellationToken cancellationToken = default);
}

public interface INotificationOrchestrator
{
    Task<ProcessEventResponse> ProcessEventAsync(NotificationEventDto notificationEvent, CancellationToken cancellationToken = default);
    Task<NotificationDeliveryResponse?> GetDeliveryStatusAsync(Guid deliveryId, CancellationToken cancellationToken = default);
    Task RetryDeliveryAsync(Guid deliveryId, CancellationToken cancellationToken = default);
}

public interface INotificationService
{
    Task<ProcessEventResponse> ProcessEventAsync(NotificationEventDto notificationEvent, CancellationToken cancellationToken = default);
    Task<ProcessEventResponse> SendManualAsync(SendManualNotificationRequest request, CancellationToken cancellationToken = default);
    Task<NotificationDeliveryResponse?> GetDeliveryStatusAsync(Guid deliveryId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationDeliveryResponse>> GetUserHistoryAsync(Guid userId, int limit, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken = default);
    Task<NotificationTemplateDto> UpdateTemplateAsync(string templateKey, string channel, UpdateTemplateRequest request, CancellationToken cancellationToken = default);
    Task RegisterPushTokenAsync(Guid userId, RegisterPushTokenRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserPreferenceDto>> GetPreferencesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task UpdatePreferenceAsync(Guid userId, UserPreferenceDto preference, CancellationToken cancellationToken = default);
    Task<NotificationStatsResponse> GetStatsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DeadLetterNotificationDto>> GetDeadLettersAsync(int limit, CancellationToken cancellationToken = default);
}
