using PrescriptionService.Domain.Enums;

namespace PrescriptionService.Domain.Entities;

public sealed class PrescriptionStatusHistory
{
    public Guid Id { get; set; }
    public Guid PrescriptionId { get; set; }
    public PrescriptionStatus FromStatus { get; set; }
    public PrescriptionStatus ToStatus { get; set; }
    public string Initiator { get; set; } = string.Empty;
    public Guid? InitiatorUserId { get; set; }
    public string? Reason { get; set; }
    public DateTime OccurredAt { get; set; }
}
