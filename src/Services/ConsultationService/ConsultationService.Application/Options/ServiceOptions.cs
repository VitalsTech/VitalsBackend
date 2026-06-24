namespace ConsultationService.Application.Options;

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
    public string RoutingDecisionTopic { get; set; } = "routing.decision";
    public string ConsultationCreatedTopic { get; set; } = "consultation.created";
    public string ConsultationCompletedTopic { get; set; } = "consultation.completed";
    public string ConsultationEmergencyTopic { get; set; } = "consultation.emergency";
    public string ConsumerGroupId { get; set; } = "consultation-service";
}

public sealed class RedisOptions
{
    public const string SectionName = "Redis";
    public bool Enabled { get; set; }
    public string ConnectionString { get; set; } = "localhost:6379";
}

public sealed class ConsultationOptions
{
    public const string SectionName = "Consultation";
    public int PatientJoinTimeoutMinutes { get; set; } = 15;
    public int DoctorJoinTimeoutUrgentMinutes { get; set; } = 5;
    public int DoctorJoinTimeoutNormalMinutes { get; set; } = 15;
    public int PauseMaxMinutes { get; set; } = 5;
    public int AsyncResponseHours { get; set; } = 24;
    public int DefaultChatDurationMinutes { get; set; } = 15;
    public int DefaultVideoDurationMinutes { get; set; } = 20;
}

public sealed class SfuOptions
{
    public const string SectionName = "Sfu";
    public bool UseStub { get; set; } = true;
    public string ServerUrl { get; set; } = "wss://sfu-stub.vitals.local";
}
