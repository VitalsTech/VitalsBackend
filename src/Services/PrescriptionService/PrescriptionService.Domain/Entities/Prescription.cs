using PrescriptionService.Domain.Enums;

namespace PrescriptionService.Domain.Entities;

public sealed class Prescription
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid? ConsultationId { get; set; }
    public PrescriptionStatus Status { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime ValidUntil { get; set; }
    public bool IsPreferential { get; set; }
    public string? PreferentialCategory { get; set; }
    public string DiagnosisForPrescription { get; set; } = string.Empty;
    public string? PharmacistComment { get; set; }
    public string? ElectronicSignature { get; set; }
    public DateTime? SignedAt { get; set; }
    public string? PharmacyOrderId { get; set; }
    public Guid? SelectedPharmacyId { get; set; }
    public int AllowedRefills { get; set; }
    public int UsedRefills { get; set; }
    public bool AutoRenewalEnabled { get; set; }
    public string PatientInstructionJson { get; set; } = "[]";
    public string ValidationResultJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<PrescriptionMedicationItem> Medications { get; set; } = new List<PrescriptionMedicationItem>();
}
