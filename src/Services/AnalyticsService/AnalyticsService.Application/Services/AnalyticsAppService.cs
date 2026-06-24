using AnalyticsService.Application.DTOs;
using AnalyticsService.Application.Interfaces;
using AnalyticsService.Domain.Entities;

namespace AnalyticsService.Application.Services;

public sealed class AnalyticsAppService : IAnalyticsService
{
    private readonly IAnalyticsRepository _repo;

    public AnalyticsAppService(IAnalyticsRepository repo) => _repo = repo;

    public async Task RecordMetricAsync(RecordMetricRequest request, CancellationToken cancellationToken = default)
    {
        await _repo.SaveMetricAsync(new AnalyticsMetric
        {
            Id = Guid.NewGuid(),
            MetricName = request.MetricName,
            EventType = request.EventType,
            Dimension = request.Dimension,
            Value = request.Value,
            PayloadJson = request.PayloadJson,
            RecordedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    public async Task<AnalyticsDashboardResponse> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddHours(-24);
        var metrics = await _repo.GetMetricsSinceAsync(since, cancellationToken);

        return new AnalyticsDashboardResponse
        {
            TotalEventsLast24h = metrics.Count,
            ConsultationsCompleted = Count(metrics, "consultation.completed"),
            PrescriptionsIssued = Count(metrics, "prescription.issued"),
            TriageCompleted = Count(metrics, "triage.completed"),
            PaymentsCompleted = Count(metrics, "payment.completed"),
            Timeline = metrics
                .OrderByDescending(x => x.RecordedAt)
                .Take(100)
                .Select(x => new MetricPointDto
                {
                    MetricName = x.MetricName,
                    Value = x.Value,
                    RecordedAt = x.RecordedAt
                })
                .ToList()
        };
    }

    private static int Count(IReadOnlyList<AnalyticsMetric> metrics, string eventType) =>
        metrics.Count(x => x.EventType.Equals(eventType, StringComparison.OrdinalIgnoreCase));
}
