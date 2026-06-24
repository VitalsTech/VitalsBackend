namespace RoutingService.Application.Options;

public sealed class JwtValidationOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "vitals-auth";
    public string Audience { get; set; } = "vitals-api";
    public string? JwksUrl { get; set; }
}

public sealed class MedicalRecordServiceOptions
{
    public const string SectionName = "MedicalRecordService";
    public string BaseUrl { get; set; } = "http://localhost:5210";
}

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";
    public bool Enabled { get; set; }
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string TriageCompletedTopic { get; set; } = "triage.completed";
    public string RoutingDecisionTopic { get; set; } = "routing.decision";
    public string LabOrderRequiredTopic { get; set; } = "lab.order_required";
    public string EmergencyRequiredTopic { get; set; } = "emergency_required";
    public string AutoResponseRequiredTopic { get; set; } = "auto_response_required";
    public string ConsumerGroupId { get; set; } = "routing-service";
}

public sealed class RoutingEngineOptions
{
    public const string SectionName = "RoutingEngine";
    public string AlgorithmVersion { get; set; } = "routing-rules-v1";
    public int MaxUrgencyWithoutEmergency { get; set; } = 4;
    public string DefaultSpecialist { get; set; } = "therapist";
    public string PediatricSpecialist { get; set; } = "pediatrician";
    public int PediatricMaxAgeYears { get; set; } = 17;
}

public sealed class RedisOptions
{
    public const string SectionName = "Redis";
    public bool Enabled { get; set; }
    public string ConnectionString { get; set; } = "localhost:6379";
}
