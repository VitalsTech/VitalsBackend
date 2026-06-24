namespace PaymentService.Application.Options;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";
    public bool Enabled { get; set; }
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string ClientId { get; set; } = "payment-service";
    public string PaymentCompletedTopic { get; set; } = "payment.completed";
    public string PaymentFailedTopic { get; set; } = "payment.failed";
}

public sealed class IntegrationServiceOptions
{
    public const string SectionName = "IntegrationService";
    public string BaseUrl { get; set; } = "http://localhost:5270";
}
