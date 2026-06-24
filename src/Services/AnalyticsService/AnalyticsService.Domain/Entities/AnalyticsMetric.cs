namespace AnalyticsService.Domain.Entities;

public sealed class AnalyticsMetric
{
    public Guid Id { get; set; }
    public string MetricName { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string? Dimension { get; set; }
    public double Value { get; set; }
    public DateTime RecordedAt { get; set; }
    public string? PayloadJson { get; set; }
}
