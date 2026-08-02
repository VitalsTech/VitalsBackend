namespace AITriageService.Application.Options;

public sealed class MedicalRecordServiceOptions
{
    public const string SectionName = "MedicalRecordService";
    public string BaseUrl { get; set; } = "http://localhost:5210";
}

public sealed class RoutingServiceOptions
{
    public const string SectionName = "RoutingService";
    public string BaseUrl { get; set; } = "http://localhost:5230";
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

    /// <summary>true — heuristic StubLlm; false — YandexGPT (или Http LLM).</summary>
    public bool UseStubModels { get; set; } = false;

    /// <summary>NER пока оставляем stub (отдельный сервис не обязателен для MVP).</summary>
    public bool UseStubNer { get; set; } = true;

    /// <summary>Yandex | Http</summary>
    public string Provider { get; set; } = "Yandex";

    public string? NerEndpoint { get; set; }
    public string? LlmEndpoint { get; set; }

    /// <summary>Yandex API-ключ (лучше через appsettings.Secrets.json / env MlServices__ApiKey).</summary>
    public string? ApiKey { get; set; }

    public string? FolderId { get; set; }
    public string? Model { get; set; }
    public string? ResponsesEndpoint { get; set; } = "https://ai.api.cloud.yandex.net/v1/responses";
    public int TimeoutSeconds { get; set; } = 60;
}
