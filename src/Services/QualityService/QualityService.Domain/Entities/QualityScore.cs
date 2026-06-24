namespace QualityService.Domain.Entities;

public sealed class QualityScore
{
    public Guid Id { get; set; }
    public Guid ConsultationId { get; set; }
    public Guid? DoctorId { get; set; }
    public double Score { get; set; }
    public string SourceEventType { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime RecordedAt { get; set; }
}
