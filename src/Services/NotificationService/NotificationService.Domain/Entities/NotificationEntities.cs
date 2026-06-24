namespace NotificationService.Domain.Entities;

public sealed class NotificationTemplate
{
    public Guid Id { get; set; }
    public string TemplateKey { get; set; } = string.Empty;
    public string Language { get; set; } = "ru";
    public string Channel { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string Body { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; }
}

public sealed class NotificationDeliveryLog
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid SourceEventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string Body { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public string? ErrorMessage { get; set; }
    public int TemplateVersion { get; set; }
    public string Priority { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? NextRetryAt { get; set; }
}

public sealed class ProcessedEvent
{
    public Guid SourceEventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
}

public sealed class DevicePushToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime RegisteredAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}

public sealed class UserNotificationPreference
{
    public Guid UserId { get; set; }
    public string Category { get; set; } = string.Empty;
    public bool PushEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; } = true;
    public bool EmailEnabled { get; set; } = true;
    public bool VoiceEnabled { get; set; } = true;
    public DateTime UpdatedAt { get; set; }
}

public sealed class DeadLetterNotification
{
    public Guid Id { get; set; }
    public Guid OriginalDeliveryId { get; set; }
    public Guid UserId { get; set; }
    public Guid SourceEventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string Body { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public int AttemptCount { get; set; }
    public string Priority { get; set; } = string.Empty;
    public DateTime MovedAt { get; set; }
}
