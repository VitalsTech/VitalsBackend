using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;

namespace NotificationService.Infrastructure.Preferences;

public sealed class UserPreferenceService : IUserPreferenceService
{
    private readonly INotificationRepository _repo;

    public UserPreferenceService(INotificationRepository repo) => _repo = repo;

    public async Task<bool> IsChannelEnabledAsync(Guid userId, string category, string channel, CancellationToken cancellationToken = default)
    {
        if (category.Equals("system", StringComparison.OrdinalIgnoreCase))
            return true;

        var pref = await _repo.GetPreferenceAsync(userId, category, cancellationToken);
        if (pref is null)
            return !channel.Equals("Voice", StringComparison.OrdinalIgnoreCase);

        return channel.ToLowerInvariant() switch
        {
            "push" => pref.PushEnabled,
            "sms" => pref.SmsEnabled,
            "email" => pref.EmailEnabled,
            "voice" => pref.VoiceEnabled,
            _ => true
        };
    }
}
