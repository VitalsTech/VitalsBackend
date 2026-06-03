namespace MedicalRecordService.Domain.Entities;

/// <summary>Append-only event in the patient stream (immutable).</summary>
public class MedicalEvent
{
    public Guid EventId { get; set; }
    public Guid PatientId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public long Version { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string SourceService { get; set; } = string.Empty;
    public Guid? CorrelationId { get; set; }
    public Guid ActorUserId { get; set; }
    public string? ActorRole { get; set; }
}
