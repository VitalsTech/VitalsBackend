using QualityService.Application.DTOs;
using QualityService.Application.Interfaces;
using QualityService.Domain.Entities;

namespace QualityService.Application.Services;

public sealed class QualityAppService : IQualityService
{
    private readonly IQualityRepository _repo;

    public QualityAppService(IQualityRepository repo) => _repo = repo;

    public Task RecordScoreAsync(QualityScore score, CancellationToken cancellationToken = default) =>
        _repo.SaveScoreAsync(score, cancellationToken);

    public async Task<QualityMetricsResponse> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddHours(-24);
        var scores = await _repo.GetScoresSinceAsync(since, cancellationToken);
        if (scores.Count == 0)
        {
            return new QualityMetricsResponse();
        }

        return new QualityMetricsResponse
        {
            SamplesLast24h = scores.Count,
            AverageScore = Math.Round(scores.Average(x => x.Score), 2),
            MinScore = scores.Min(x => x.Score),
            MaxScore = scores.Max(x => x.Score),
            RecentScores = scores
                .OrderByDescending(x => x.RecordedAt)
                .Take(50)
                .Select(x => new QualityScoreDto
                {
                    ConsultationId = x.ConsultationId,
                    DoctorId = x.DoctorId,
                    Score = x.Score,
                    SourceEventType = x.SourceEventType,
                    RecordedAt = x.RecordedAt
                })
                .ToList()
        };
    }
}
