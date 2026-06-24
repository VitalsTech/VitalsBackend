namespace PrescriptionService.Application.DTOs;

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

public sealed class CreatePrescriptionRequest
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

public sealed class ValidationIssueDto
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
}

public sealed class PrescriptionValidationResultDto
{
    public string Outcome { get; set; } = string.Empty;
    public IReadOnlyList<ValidationIssueDto> Issues { get; set; } = Array.Empty<ValidationIssueDto>();
}

public sealed class PrescriptionResponse
{
    public Guid PrescriptionId { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid? ConsultationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public DateTime ValidUntil { get; set; }
    public bool IsPreferential { get; set; }
    public string DiagnosisForPrescription { get; set; } = string.Empty;
    public int AllowedRefills { get; set; }
    public int UsedRefills { get; set; }
    public string? PharmacyOrderId { get; set; }
    public Guid? SelectedPharmacyId { get; set; }
    public bool IsSigned { get; set; }
    public IReadOnlyList<MedicationItemDto> Medications { get; set; } = Array.Empty<MedicationItemDto>();
    public PrescriptionValidationResultDto? LastValidation { get; set; }
}

public sealed class SendToPharmacyRequest
{
    public Guid PharmacyId { get; set; }
    public bool AutoSelectNearest { get; set; }
}

public sealed class CancelPrescriptionRequest
{
    public string Reason { get; set; } = string.Empty;
}

public sealed class FulfillmentRequest
{
    public bool Partial { get; set; }
    public int PackagesDispensed { get; set; } = 1;
    public string? PharmacyOrderId { get; set; }
}

public sealed class QrCodeResponse
{
    public string PrescriptionId { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string QrCodeBase64Png { get; set; } = string.Empty;
}

public sealed class PatientInstructionDto
{
    public string MedicationName { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public string TimingRelativeToFood { get; set; } = string.Empty;
    public int CourseDays { get; set; }
    public IReadOnlyList<string> SpecialNotes { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();
}

public sealed class PatientMedicalContextDto
{
    public IReadOnlyList<string> Allergies { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> ActiveMedications { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> ActiveDiagnoses { get; set; } = Array.Empty<string>();
    public int? AgeYears { get; set; }
    public bool IsPregnant { get; set; }
}

public sealed class PharmacyDispatchResultDto
{
    public bool Success { get; set; }
    public string? OrderId { get; set; }
    public string? Message { get; set; }
}
