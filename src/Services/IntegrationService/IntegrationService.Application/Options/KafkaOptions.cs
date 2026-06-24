namespace IntegrationService.Application.Options;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";
    public bool Enabled { get; set; }
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string ConsumerGroupId { get; set; } = "integration-service";
    public string ClientId { get; set; } = "integration-service";
    public string LabOrderRequiredTopic { get; set; } = "lab.order_required";
    public string EmergencyRequiredTopic { get; set; } = "emergency_required";
}
