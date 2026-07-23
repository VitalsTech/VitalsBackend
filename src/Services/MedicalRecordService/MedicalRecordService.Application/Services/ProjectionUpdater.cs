using System.Text.Json;
using MedicalRecordService.Domain.Entities;
using MedicalRecordService.Domain.Enums;
using MedicalRecordService.Domain.Interfaces;

namespace MedicalRecordService.Application.Services;

public sealed class ProjectionUpdater
{
    private readonly IProjectionRepository _projections;

    public ProjectionUpdater(IProjectionRepository projections) => _projections = projections;

    public async Task ApplyAsync(MedicalEvent medicalEvent, CancellationToken cancellationToken = default)
    {
        using var doc = JsonDocument.Parse(medicalEvent.PayloadJson);
        var root = doc.RootElement;

        switch (medicalEvent.EventType)
        {
            case MedicalEventTypes.DiagnosisConfirmed:
                await _projections.ApplyDiagnosisAsync(new PatientDiagnosisProjection
                {
                    PatientId = medicalEvent.PatientId,
                    SourceEventId = medicalEvent.EventId,
                    Icd10Code = GetString(root, "icd10Code") ?? "",
                    Description = GetString(root, "description") ?? "",
                    IsActive = true,
                    RecordedAt = medicalEvent.OccurredAt
                }, cancellationToken);
                break;

            case MedicalEventTypes.DiagnosisRevised:
                await _projections.SupersedeDiagnosisAsync(
                    medicalEvent.PatientId,
                    medicalEvent.EventId,
                    GetString(root, "previousIcd10Code"),
                    cancellationToken);
                await _projections.ApplyDiagnosisAsync(new PatientDiagnosisProjection
                {
                    PatientId = medicalEvent.PatientId,
                    SourceEventId = medicalEvent.EventId,
                    Icd10Code = GetString(root, "icd10Code") ?? "",
                    Description = GetString(root, "description") ?? "",
                    IsActive = true,
                    RecordedAt = medicalEvent.OccurredAt
                }, cancellationToken);
                break;

            case MedicalEventTypes.PrescriptionIssued:
                await _projections.ApplyPrescriptionAsync(new PatientPrescriptionProjection
                {
                    PatientId = medicalEvent.PatientId,
                    SourceEventId = medicalEvent.EventId,
                    MedicationName = GetString(root, "medicationName") ?? "",
                    Dosage = GetString(root, "dosage"),
                    Instructions = GetString(root, "instructions"),
                    IsActive = true,
                    PrescribedAt = medicalEvent.OccurredAt
                }, cancellationToken);
                break;

            case MedicalEventTypes.PrescriptionRevoked:
                var prescriptionEventId = GetGuid(root, "prescriptionEventId");
                if (prescriptionEventId.HasValue)
                    await _projections.RevokePrescriptionAsync(medicalEvent.PatientId, prescriptionEventId.Value, cancellationToken);
                break;

            case MedicalEventTypes.AllergyRecorded:
                await _projections.ApplyAllergyAsync(new PatientAllergyProjection
                {
                    PatientId = medicalEvent.PatientId,
                    SourceEventId = medicalEvent.EventId,
                    Allergen = GetString(root, "allergen") ?? "",
                    Severity = GetString(root, "severity"),
                    IsActive = true,
                    RecordedAt = medicalEvent.OccurredAt
                }, cancellationToken);
                break;

            case MedicalEventTypes.ImmunizationRecorded:
                await _projections.ApplyImmunizationAsync(new PatientImmunizationProjection
                {
                    PatientId = medicalEvent.PatientId,
                    SourceEventId = medicalEvent.EventId,
                    VaccineName = GetString(root, "vaccineName") ?? "",
                    AdministeredAt = GetDateTime(root, "administeredAt") ?? medicalEvent.OccurredAt
                }, cancellationToken);
                break;

            case MedicalEventTypes.MoodCheck:
            case MedicalEventTypes.VitalSignRecorded:
                await _projections.ApplyVitalSignAsync(new PatientVitalSignProjection
                {
                    PatientId = medicalEvent.PatientId,
                    SourceEventId = medicalEvent.EventId,
                    VitalType = GetString(root, "vitalType") ?? "mood",
                    Value = GetString(root, "value") ?? GetString(root, "mood") ?? "",
                    Unit = GetString(root, "unit"),
                    RecordedAt = medicalEvent.OccurredAt
                }, cancellationToken);
                break;

            case MedicalEventTypes.LabResultReceived:
                await _projections.ApplyLabResultAsync(new PatientLabResultProjection
                {
                    PatientId = medicalEvent.PatientId,
                    SourceEventId = medicalEvent.EventId,
                    TestName = GetString(root, "testName") ?? "",
                    ResultValue = GetString(root, "resultValue") ?? "",
                    ReferenceRange = GetString(root, "referenceRange"),
                    IsCritical = GetBool(root, "isCritical"),
                    ReceivedAt = medicalEvent.OccurredAt
                }, cancellationToken);
                break;
        }

        await _projections.SaveChangesAsync(cancellationToken);
    }

    private static string? GetString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;

    private static bool GetBool(JsonElement root, string name) =>
        root.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.True;

    private static Guid? GetGuid(JsonElement root, string name) =>
        root.TryGetProperty(name, out var p) && p.TryGetGuid(out var g) ? g : null;

    private static DateTime? GetDateTime(JsonElement root, string name) =>
        root.TryGetProperty(name, out var p) && p.TryGetDateTime(out var dt) ? dt : null;
}
