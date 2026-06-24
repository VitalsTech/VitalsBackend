namespace ApiGateway.Application.DTOs.Prescriptions;

public sealed class MedicationItemDto
{
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
    public bool RequiresPrescription { get; set; } = true;
    public string? MaxDailyDose { get; set; }
}

public sealed class CreatePrescriptionRequestDto
{
    public Guid PatientId { get; set; }
    public Guid? ConsultationId { get; set; }
    public string DiagnosisForPrescription { get; set; } = string.Empty;
    public bool IsPreferential { get; set; }
    public string? PreferentialCategory { get; set; }
    public int AllowedRefills { get; set; }
    public bool AutoRenewalEnabled { get; set; }
    public string? PharmacistComment { get; set; }
    public IReadOnlyList<MedicationItemDto> Medications { get; set; } = Array.Empty<MedicationItemDto>();
    public bool ConfirmWarnings { get; set; }
}

public sealed class SendToPharmacyRequestDto
{
    public Guid PharmacyId { get; set; }
    public bool AutoSelectNearest { get; set; }
}

public sealed class CancelPrescriptionRequestDto
{
    public string Reason { get; set; } = string.Empty;
}
