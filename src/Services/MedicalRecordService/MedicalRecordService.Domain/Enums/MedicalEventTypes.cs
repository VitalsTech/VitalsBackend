namespace MedicalRecordService.Domain.Enums;

public static class MedicalEventTypes
{
    public const string PatientRequestCreated = "PatientRequestCreated";
    public const string AiTriageUrgencyDetermined = "AiTriageUrgencyDetermined";
    public const string DiagnosisConfirmed = "DiagnosisConfirmed";
    public const string DiagnosisRevised = "DiagnosisRevised";
    public const string PrescriptionIssued = "PrescriptionIssued";
    public const string PrescriptionRevoked = "PrescriptionRevoked";
    public const string LabResultReceived = "LabResultReceived";
    public const string TreatmentStarted = "TreatmentStarted";
    public const string TreatmentCompleted = "TreatmentCompleted";
    public const string AllergyRecorded = "AllergyRecorded";
    public const string VitalSignRecorded = "VitalSignRecorded";
    public const string ImmunizationRecorded = "ImmunizationRecorded";
}
