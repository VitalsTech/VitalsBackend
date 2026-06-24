namespace NotificationService.Application.Options;

public sealed class JwtValidationOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "vitals-auth";
    public string Audience { get; set; } = "vitals-api";
    public string? JwksUrl { get; set; }
}

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";
    public bool Enabled { get; set; }
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string ConsumerGroupId { get; set; } = "notification-service";
    public IReadOnlyList<string> Topics { get; set; } = Array.Empty<string>();
}

public sealed class UserServiceOptions
{
    public const string SectionName = "UserService";
    public string BaseUrl { get; set; } = "http://localhost:5195";
}

public sealed class IntegrationServiceOptions
{
    public const string SectionName = "IntegrationService";
    public string BaseUrl { get; set; } = "http://localhost:5270";
    public bool UseStub { get; set; } = true;
}

public sealed class NotificationOptions
{
    public const string SectionName = "Notification";
    public int MaxRetryAttempts { get; set; } = 5;
    public int CriticalMaxRetryAttempts { get; set; } = 10;
    public int ProcessedEventRetentionDays { get; set; } = 7;
}

public sealed class RedisOptions
{
    public const string SectionName = "Redis";
    public bool Enabled { get; set; }
    public string ConnectionString { get; set; } = "localhost:6379";
}
