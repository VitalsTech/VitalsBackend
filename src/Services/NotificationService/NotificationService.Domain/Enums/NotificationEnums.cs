namespace NotificationService.Domain.Enums;

public enum NotificationChannel
{
    Push = 1,
    Sms = 2,
    Email = 3,
    Voice = 4
}

public enum DeliveryStatus
{
    Pending = 1,
    Delivered = 2,
    Failed = 3,
    Duplicate = 4,
    SkippedByPreferences = 5
}

public enum NotificationPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}
