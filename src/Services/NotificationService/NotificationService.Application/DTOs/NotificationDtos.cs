namespace NotificationService.Application.DTOs;

public sealed class NotificationEventDto
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public Guid? SecondaryUserId { get; set; }
    public string Priority { get; set; } = "Medium";
    public string Category { get; set; } = "system";
    public Dictionary<string, string> TemplateData { get; set; } = new();
}

public sealed class SendManualNotificationRequest
{
    public Guid UserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium";
    public string Category { get; set; } = "system";
    public IReadOnlyList<string> Channels { get; set; } = Array.Empty<string>();
    public Dictionary<string, string> TemplateData { get; set; } = new();
}

public sealed class NotificationDeliveryResponse
{
    public Guid DeliveryId { get; set; }
    public Guid UserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
}

public sealed class ProcessEventResponse
{
    public Guid SourceEventId { get; set; }
    public bool WasDuplicate { get; set; }
    public IReadOnlyList<NotificationDeliveryResponse> Deliveries { get; set; } = Array.Empty<NotificationDeliveryResponse>();
}

public sealed class NotificationTemplateDto
{
    public string TemplateKey { get; set; } = string.Empty;
    public string Language { get; set; } = "ru";
    public string Channel { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string Body { get; set; } = string.Empty;
    public int Version { get; set; }
}

public sealed class UpdateTemplateRequest
{
    public string? Subject { get; set; }
    public string Body { get; set; } = string.Empty;
}

public sealed class RegisterPushTokenRequest
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

public sealed class NotificationStatsResponse
{
    public int TotalLastHour { get; set; }
    public int DeliveredLastHour { get; set; }
    public int FailedLastHour { get; set; }
    public double DeliveryRatePercent { get; set; }
}

public sealed class DeadLetterNotificationDto
{
    public Guid Id { get; set; }
    public Guid OriginalDeliveryId { get; set; }
    public Guid UserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public int AttemptCount { get; set; }
    public DateTime MovedAt { get; set; }
}

public sealed class RenderedMessage
{
    public string? Subject { get; set; }
    public string Body { get; set; } = string.Empty;
    public int TemplateVersion { get; set; }
}

public sealed class ChannelDispatchResult
{
    public bool Success { get; set; }
    public string? ProviderMessageId { get; set; }
    public string? ErrorMessage { get; set; }
}
