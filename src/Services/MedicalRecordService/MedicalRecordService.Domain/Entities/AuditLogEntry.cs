namespace MedicalRecordService.Domain.Entities;

public class AuditLogEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PatientId { get; set; }
    public Guid ActorUserId { get; set; }
    public string ActorType { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public Guid? MedicalEventId { get; set; }
    public string? DetailsJson { get; set; }
    public string? IpAddress { get; set; }
    public string? SessionId { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
