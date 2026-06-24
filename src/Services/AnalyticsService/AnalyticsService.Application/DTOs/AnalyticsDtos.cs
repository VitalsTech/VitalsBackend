namespace AnalyticsService.Application.DTOs;

public sealed class AnalyticsDashboardResponse
{
    public int TotalEventsLast24h { get; set; }
    public int ConsultationsCompleted { get; set; }
    public int PrescriptionsIssued { get; set; }
    public int TriageCompleted { get; set; }
    public int PaymentsCompleted { get; set; }
    public IReadOnlyList<MetricPointDto> Timeline { get; set; } = Array.Empty<MetricPointDto>();
}

public sealed class MetricPointDto
{
    public string MetricName { get; set; } = string.Empty;
    public double Value { get; set; }
    public DateTime RecordedAt { get; set; }
}

public sealed class RecordMetricRequest
{
    public string MetricName { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string? Dimension { get; set; }
    public double Value { get; set; } = 1;
    public string? PayloadJson { get; set; }
}
