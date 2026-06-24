using AnalyticsService.Application.Interfaces;
using AnalyticsService.Domain.Entities;
using AnalyticsService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AnalyticsService.Infrastructure.Repositories;

public sealed class AnalyticsRepository : IAnalyticsRepository
{
    private readonly AnalyticsDbContext _db;

    public AnalyticsRepository(AnalyticsDbContext db) => _db = db;

    public async Task SaveMetricAsync(AnalyticsMetric metric, CancellationToken cancellationToken = default)
    {
        _db.Metrics.Add(metric);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<IReadOnlyList<AnalyticsMetric>> GetMetricsSinceAsync(DateTime since, CancellationToken cancellationToken = default) =>
        _db.Metrics.AsNoTracking()
            .Where(x => x.RecordedAt >= since)
            .OrderByDescending(x => x.RecordedAt)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<AnalyticsMetric>)t.Result, cancellationToken);
}
