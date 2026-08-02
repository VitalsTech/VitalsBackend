using NotificationService.Application.DTOs;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;
using NotificationService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace NotificationService.Infrastructure.Repositories;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly NotificationDbContext _db;

    public NotificationRepository(NotificationDbContext db) => _db = db;

    public Task<bool> IsEventProcessedAsync(Guid eventId, CancellationToken cancellationToken = default) =>
        _db.ProcessedEvents.AsNoTracking().AnyAsync(x => x.SourceEventId == eventId, cancellationToken);

    public async Task MarkEventProcessedAsync(Guid eventId, string eventType, CancellationToken cancellationToken = default)
    {
        _db.ProcessedEvents.Add(new ProcessedEvent
        {
            SourceEventId = eventId,
            EventType = eventType,
            ProcessedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<NotificationTemplate?> GetTemplateAsync(string templateKey, string channel, string language, CancellationToken cancellationToken = default) =>
        _db.Templates.AsNoTracking()
            .Where(x => x.TemplateKey == templateKey && x.Channel == channel && x.Language == language && x.IsActive)
            .OrderByDescending(x => x.Version)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<IReadOnlyList<NotificationTemplate>> GetAllTemplatesAsync(CancellationToken cancellationToken = default) =>
        _db.Templates.AsNoTracking().Where(x => x.IsActive).ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<NotificationTemplate>)t.Result, cancellationToken);

    public async Task SaveTemplateAsync(NotificationTemplate template, CancellationToken cancellationToken = default)
    {
        if (_db.Entry(template).State == EntityState.Detached)
            _db.Templates.Add(template);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveDeliveryLogAsync(NotificationDeliveryLog log, CancellationToken cancellationToken = default)
    {
        if (_db.Entry(log).State == EntityState.Detached)
        {
            var tracked = await _db.DeliveryLogs.FindAsync([log.Id], cancellationToken);
            if (tracked is null)
                _db.DeliveryLogs.Add(log);
            else
                _db.Entry(tracked).CurrentValues.SetValues(log);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<NotificationDeliveryLog?> GetDeliveryByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.DeliveryLogs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<NotificationDeliveryLog>> GetUserHistoryAsync(IReadOnlyList<Guid> userIds, int limit, CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0)
            return Array.Empty<NotificationDeliveryLog>();

        return await _db.DeliveryLogs.AsNoTracking()
            .Where(x => userIds.Contains(x.UserId))
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationDeliveryLog>> GetPendingRetriesAsync(DateTime utcNow, CancellationToken cancellationToken = default) =>
        await _db.DeliveryLogs
            .Where(x => x.Status == "Failed" && x.NextRetryAt != null && x.NextRetryAt <= utcNow)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DevicePushToken>> GetActivePushTokensAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await _db.PushTokens.AsNoTracking()
            .Where(x => x.UserId == userId && x.IsActive)
            .ToListAsync(cancellationToken);

    public async Task SavePushTokenAsync(DevicePushToken token, CancellationToken cancellationToken = default)
    {
        var existing = await _db.PushTokens.FirstOrDefaultAsync(x => x.UserId == token.UserId && x.Token == token.Token, cancellationToken);
        if (existing is not null)
        {
            existing.IsActive = true;
            existing.LastUsedAt = DateTime.UtcNow;
        }
        else
        {
            _db.PushTokens.Add(token);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<UserNotificationPreference?> GetPreferenceAsync(Guid userId, string category, CancellationToken cancellationToken = default) =>
        _db.Preferences.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId && x.Category == category, cancellationToken);

    public async Task SavePreferenceAsync(UserNotificationPreference preference, CancellationToken cancellationToken = default)
    {
        var existing = await _db.Preferences.FindAsync([preference.UserId, preference.Category], cancellationToken);
        if (existing is null)
            _db.Preferences.Add(preference);
        else
            _db.Entry(existing).CurrentValues.SetValues(preference);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<NotificationStatsResponse> GetStatsAsync(DateTime since, CancellationToken cancellationToken = default)
    {
        var logs = await _db.DeliveryLogs.AsNoTracking().Where(x => x.CreatedAt >= since).ToListAsync(cancellationToken);
        var total = logs.Count;
        var delivered = logs.Count(x => x.Status == "Delivered");
        var failed = logs.Count(x => x.Status == "Failed");

        return new NotificationStatsResponse
        {
            TotalLastHour = total,
            DeliveredLastHour = delivered,
            FailedLastHour = failed,
            DeliveryRatePercent = total == 0 ? 100 : Math.Round(delivered * 100.0 / total, 2)
        };
    }

    public async Task CleanupOldProcessedEventsAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        var old = await _db.ProcessedEvents.Where(x => x.ProcessedAt < olderThan).ToListAsync(cancellationToken);
        _db.ProcessedEvents.RemoveRange(old);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveDeadLetterAsync(DeadLetterNotification entry, CancellationToken cancellationToken = default)
    {
        _db.DeadLetters.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DeadLetterNotification>> GetDeadLettersAsync(int limit, CancellationToken cancellationToken = default) =>
        await _db.DeadLetters.AsNoTracking()
            .OrderByDescending(x => x.MovedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
}
