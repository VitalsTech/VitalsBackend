namespace PrescriptionService.Domain.Entities;

public sealed class PrescriptionMedicationItem
{
    public Guid Id { get; set; }
    public Guid PrescriptionId { get; set; }
    public string TradeName { get; set; } = string.Empty;
    public string Inn { get; set; } = string.Empty;
    public string DosageForm { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string PackageQuantity { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public int CourseDays { get; set; }
    public string? SpecialInstructions { get; set; }
    public string AtcCode { get; set; } = string.Empty;
    public bool RequiresPrescription { get; set; }
    public string? MaxDailyDose { get; set; }

    public Prescription? Prescription { get; set; }
}
