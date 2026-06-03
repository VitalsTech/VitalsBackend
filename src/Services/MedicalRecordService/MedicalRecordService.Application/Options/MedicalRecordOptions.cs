namespace MedicalRecordService.Application.Options;

public sealed class MedicalRecordOptions
{
    public const string SectionName = "MedicalRecord";

    public int SnapshotEveryEvents { get; set; } = 100;
}

public sealed class JwtValidationOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "vitals-auth";
    public string Audience { get; set; } = "vitals-api";
    public string? RsaPublicKeyPem { get; set; }
    public bool AllowDevelopmentHeaderFallback { get; set; } = true;
}

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    public bool Enabled { get; set; }
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string MedicalEventsTopic { get; set; } = "medical-record.events";
    public string IntegrationInboundTopic { get; set; } = "integration.lab-results";
}
