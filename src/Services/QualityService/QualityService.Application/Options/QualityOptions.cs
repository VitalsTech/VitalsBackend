namespace QualityService.Application.Options;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";
    public bool Enabled { get; set; }
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string ConsumerGroupId { get; set; } = "quality-service";
    public string ClientId { get; set; } = "quality-service";
    public string ConsultationCompletedTopic { get; set; } = "consultation.completed";
}
