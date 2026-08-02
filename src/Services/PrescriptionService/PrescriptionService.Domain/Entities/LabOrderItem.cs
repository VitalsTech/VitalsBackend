namespace PrescriptionService.Domain.Entities;

public sealed class LabOrderItem
{
    public Guid Id { get; set; }
    public Guid LabOrderId { get; set; }
    public LabOrder? LabOrder { get; set; }
    public string TestName { get; set; } = string.Empty;
    public string? TestCode { get; set; }
    public string? SpecimenType { get; set; }
    public string? SpecialInstructions { get; set; }
    public string? ResultValue { get; set; }
    public string? ReferenceRange { get; set; }
    public string? Unit { get; set; }
    public bool IsCritical { get; set; }
    public DateTime? ResultReceivedAt { get; set; }
    public string? ResultAttachmentUrl { get; set; }
    public string? ResultComment { get; set; }
}
