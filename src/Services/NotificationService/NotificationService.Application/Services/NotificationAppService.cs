using NotificationService.Application.DTOs;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;

namespace NotificationService.Application.Services;

public sealed class NotificationAppService : INotificationService
{
    private readonly INotificationOrchestrator _orchestrator;
    private readonly INotificationRepository _repo;

    public NotificationAppService(INotificationOrchestrator orchestrator, INotificationRepository repo)
    {
        _orchestrator = orchestrator;
        _repo = repo;
    }

    public Task<ProcessEventResponse> ProcessEventAsync(NotificationEventDto notificationEvent, CancellationToken cancellationToken = default) =>
        _orchestrator.ProcessEventAsync(notificationEvent, cancellationToken);

    public async Task<ProcessEventResponse> SendManualAsync(SendManualNotificationRequest request, CancellationToken cancellationToken = default)
    {
        var evt = new NotificationEventDto
        {
            EventId = Guid.NewGuid(),
            EventType = request.EventType,
            UserId = request.UserId,
            Priority = request.Priority,
            Category = request.Category,
            TemplateData = request.TemplateData.ToDictionary(x => x.Key, x => x.Value)
        };

        if (request.Channels.Count > 0)
            evt.TemplateData["manual_channels"] = string.Join(',', request.Channels);

        return await _orchestrator.ProcessEventAsync(evt, cancellationToken);
    }

    public Task<NotificationDeliveryResponse?> GetDeliveryStatusAsync(Guid deliveryId, CancellationToken cancellationToken = default) =>
        _orchestrator.GetDeliveryStatusAsync(deliveryId, cancellationToken);

    public async Task<IReadOnlyList<NotificationDeliveryResponse>> GetUserHistoryAsync(Guid userId, int limit, CancellationToken cancellationToken = default)
    {
        var logs = await _repo.GetUserHistoryAsync(userId, limit, cancellationToken);
        return logs.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<NotificationTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken = default)
    {
        var templates = await _repo.GetAllTemplatesAsync(cancellationToken);
        return templates.Select(t => new NotificationTemplateDto
        {
            TemplateKey = t.TemplateKey,
            Language = t.Language,
            Channel = t.Channel,
            Subject = t.Subject,
            Body = t.Body,
            Version = t.Version
        }).ToList();
    }

    public async Task<NotificationTemplateDto> UpdateTemplateAsync(
        string templateKey,
        string channel,
        UpdateTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var existing = await _repo.GetTemplateAsync(templateKey, channel, "ru", cancellationToken)
            ?? throw new KeyNotFoundException("Template not found.");

        existing.IsActive = false;
        await _repo.SaveTemplateAsync(existing, cancellationToken);

        var updated = new NotificationTemplate
        {
            Id = Guid.NewGuid(),
            TemplateKey = templateKey,
            Language = "ru",
            Channel = channel,
            Subject = request.Subject ?? existing.Subject,
            Body = request.Body,
            Version = existing.Version + 1,
            IsActive = true,
            UpdatedAt = DateTime.UtcNow
        };
        await _repo.SaveTemplateAsync(updated, cancellationToken);

        return new NotificationTemplateDto
        {
            TemplateKey = updated.TemplateKey,
            Language = updated.Language,
            Channel = updated.Channel,
            Subject = updated.Subject,
            Body = updated.Body,
            Version = updated.Version
        };
    }

    public async Task RegisterPushTokenAsync(Guid userId, RegisterPushTokenRequest request, CancellationToken cancellationToken = default)
    {
        await _repo.SavePushTokenAsync(new DevicePushToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Platform = request.Platform,
            Token = request.Token,
            IsActive = true,
            RegisteredAt = DateTime.UtcNow
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<UserPreferenceDto>> GetPreferencesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var categories = new[] { "consultations", "messages", "labs", "prescriptions", "payments", "marketing", "system" };
        var result = new List<UserPreferenceDto>();

        foreach (var category in categories)
        {
            var pref = await _repo.GetPreferenceAsync(userId, category, cancellationToken);
            result.Add(pref is null
                ? new UserPreferenceDto { Category = category, PushEnabled = true, SmsEnabled = category != "marketing", EmailEnabled = category != "messages" && category != "marketing" }
                : new UserPreferenceDto
                {
                    Category = pref.Category,
                    PushEnabled = pref.PushEnabled,
                    SmsEnabled = pref.SmsEnabled,
                    EmailEnabled = pref.EmailEnabled,
                    VoiceEnabled = pref.VoiceEnabled
                });
        }

        return result;
    }

    public async Task UpdatePreferenceAsync(Guid userId, UserPreferenceDto preference, CancellationToken cancellationToken = default)
    {
        if (preference.Category.Equals("system", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("System notification preferences cannot be disabled.");

        await _repo.SavePreferenceAsync(new UserNotificationPreference
        {
            UserId = userId,
            Category = preference.Category,
            PushEnabled = preference.PushEnabled,
            SmsEnabled = preference.SmsEnabled,
            EmailEnabled = preference.EmailEnabled,
            VoiceEnabled = preference.VoiceEnabled,
            UpdatedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    public Task<NotificationStatsResponse> GetStatsAsync(CancellationToken cancellationToken = default) =>
        _repo.GetStatsAsync(DateTime.UtcNow.AddHours(-1), cancellationToken);

    private static NotificationDeliveryResponse Map(NotificationDeliveryLog log) => new()
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
