namespace Vitals.Messaging;

public sealed class KafkaConnectionOptions
{
    public const string SectionName = "Kafka";
    public bool Enabled { get; set; }
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string ConsumerGroupId { get; set; } = "vitals-service";
    public string ClientId { get; set; } = "vitals-client";
}
