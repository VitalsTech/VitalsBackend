using QualityService.Application.DTOs;
using QualityService.Domain.Entities;

namespace QualityService.Application.Interfaces;

public interface IQualityRepository
{
    Task SaveScoreAsync(QualityScore score, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<QualityScore>> GetScoresSinceAsync(DateTime since, CancellationToken cancellationToken = default);
}

public interface IQualityService
{
    Task RecordScoreAsync(QualityScore score, CancellationToken cancellationToken = default);
    Task<QualityMetricsResponse> GetMetricsAsync(CancellationToken cancellationToken = default);
}
