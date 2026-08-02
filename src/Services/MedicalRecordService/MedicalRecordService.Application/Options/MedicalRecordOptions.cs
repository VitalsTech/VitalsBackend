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
    public string PatientMoodUpdatedTopic { get; set; } = "patient.mood.updated";
    public string PatientTriageCompletedTopic { get; set; } = "patient.triage.completed";
    public string IntegrationInboundTopic { get; set; } = "integration.lab-results";
}

public sealed class ConsultationServiceOptions
{
    public const string SectionName = "ConsultationService";
    public string BaseUrl { get; set; } = "http://consultation";
}

public sealed class UserServiceOptions
{
    public const string SectionName = "UserService";
    public string BaseUrl { get; set; } = "http://userservice";
}
