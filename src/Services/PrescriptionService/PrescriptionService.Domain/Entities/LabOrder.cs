using PrescriptionService.Domain.Enums;

namespace PrescriptionService.Domain.Entities;

public sealed class LabOrder
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid? ConsultationId { get; set; }
    public LabOrderStatus Status { get; set; }
    public DateTime OrderedAt { get; set; }
    public string? ClinicalIndication { get; set; }
    public string Priority { get; set; } = "routine";
    public string? DoctorComment { get; set; }
    public string? ExternalLabOrderId { get; set; }
    public string? CancelReason { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<LabOrderItem> Items { get; set; } = new List<LabOrderItem>();
}
