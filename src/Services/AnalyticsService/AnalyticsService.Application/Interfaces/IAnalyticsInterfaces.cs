using AnalyticsService.Application.DTOs;
using AnalyticsService.Domain.Entities;

namespace AnalyticsService.Application.Interfaces;

public interface IAnalyticsRepository
{
    Task SaveMetricAsync(AnalyticsMetric metric, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AnalyticsMetric>> GetMetricsSinceAsync(DateTime since, CancellationToken cancellationToken = default);
}

public interface IAnalyticsService
{
    Task RecordMetricAsync(RecordMetricRequest request, CancellationToken cancellationToken = default);
    Task<AnalyticsDashboardResponse> GetDashboardAsync(CancellationToken cancellationToken = default);
}
