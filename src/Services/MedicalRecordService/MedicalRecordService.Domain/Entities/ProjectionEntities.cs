namespace MedicalRecordService.Domain.Entities;

public class PatientDiagnosisProjection
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PatientId { get; set; }
    public Guid SourceEventId { get; set; }
    public string Icd10Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public Guid? SupersededByEventId { get; set; }
    public DateTime RecordedAt { get; set; }
}

public class PatientPrescriptionProjection
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PatientId { get; set; }
    public Guid SourceEventId { get; set; }
    public string MedicationName { get; set; } = string.Empty;
    public string? Dosage { get; set; }
    public string? Instructions { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime PrescribedAt { get; set; }
}

public class PatientAllergyProjection
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PatientId { get; set; }
    public Guid SourceEventId { get; set; }
    public string Allergen { get; set; } = string.Empty;
    public string? Severity { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime RecordedAt { get; set; }
}

public class PatientImmunizationProjection
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PatientId { get; set; }
    public Guid SourceEventId { get; set; }
    public string VaccineName { get; set; } = string.Empty;
    public DateTime AdministeredAt { get; set; }
}

public class PatientVitalSignProjection
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PatientId { get; set; }
    public Guid SourceEventId { get; set; }
    public string VitalType { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public DateTime RecordedAt { get; set; }
}

public class PatientLabResultProjection
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PatientId { get; set; }
    public Guid SourceEventId { get; set; }
    public string TestName { get; set; } = string.Empty;
    public string ResultValue { get; set; } = string.Empty;
    public string? ReferenceRange { get; set; }
    public bool IsCritical { get; set; }
    public DateTime ReceivedAt { get; set; }
}
