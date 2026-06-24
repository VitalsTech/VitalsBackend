namespace QualityService.Application.DTOs;

public sealed class QualityMetricsResponse
{
    public int SamplesLast24h { get; set; }
    public double AverageScore { get; set; }
    public double MinScore { get; set; }
    public double MaxScore { get; set; }
    public IReadOnlyList<QualityScoreDto> RecentScores { get; set; } = Array.Empty<QualityScoreDto>();
}

public sealed class QualityScoreDto
{
    public Guid ConsultationId { get; set; }
    public Guid? DoctorId { get; set; }
    public double Score { get; set; }
    public string SourceEventType { get; set; } = string.Empty;
    public DateTime RecordedAt { get; set; }
}
