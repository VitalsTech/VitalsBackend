namespace AnalyticsService.Application.Options;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";
    public bool Enabled { get; set; }
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string ConsumerGroupId { get; set; } = "analytics-service";
    public string ClientId { get; set; } = "analytics-service";
    public string ConsultationCompletedTopic { get; set; } = "consultation.completed";
    public string PrescriptionIssuedTopic { get; set; } = "prescription.issued";
    public string TriageCompletedTopic { get; set; } = "triage.completed";
    public string PaymentCompletedTopic { get; set; } = "payment.completed";
}
