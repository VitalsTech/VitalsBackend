using PrescriptionService.Domain.Enums;

namespace PrescriptionService.Domain.Entities;

public sealed class LabOrderStatusHistory
{
    public Guid Id { get; set; }
    public Guid LabOrderId { get; set; }
    public LabOrderStatus FromStatus { get; set; }
    public LabOrderStatus ToStatus { get; set; }
    public string Initiator { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime OccurredAt { get; set; }
}
