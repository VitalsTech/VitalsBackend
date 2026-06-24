using QualityService.Application.Interfaces;
using QualityService.Domain.Entities;
using QualityService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace QualityService.Infrastructure.Repositories;

public sealed class QualityRepository : IQualityRepository
{
    private readonly QualityDbContext _db;

    public QualityRepository(QualityDbContext db) => _db = db;

    public async Task SaveScoreAsync(QualityScore score, CancellationToken cancellationToken = default)
    {
        _db.Scores.Add(score);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<IReadOnlyList<QualityScore>> GetScoresSinceAsync(DateTime since, CancellationToken cancellationToken = default) =>
        _db.Scores.AsNoTracking()
            .Where(x => x.RecordedAt >= since)
            .OrderByDescending(x => x.RecordedAt)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<QualityScore>)t.Result, cancellationToken);
}
