namespace AITriageService.Application.Options;

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
    public string ClientId { get; set; } = "ai-triage-service";
}

public sealed class JwtValidationOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "vitals-auth";
    public string Audience { get; set; } = "vitals-api";
    public string? JwksUrl { get; set; }
}

public sealed class MlServicesOptions
{
    public const string SectionName = "MlServices";
    public bool UseStubModels { get; set; } = true;
    public string? NerEndpoint { get; set; }
    public string? LlmEndpoint { get; set; }
    public string? ApiKey { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
}
