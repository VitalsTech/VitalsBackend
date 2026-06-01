using MedicalRecordService.Application.DTOs;
using MedicalRecordService.Domain.Interfaces;

namespace MedicalRecordService.Application.Services;

public static class PatientRecordMapper
{
    public static PatientCurrentStateDto ToDto(PatientStateProjections state) => new()
    {
        ActiveDiagnoses = state.Diagnoses
            .Where(d => d.IsActive)
            .Select(d => new DiagnosisDto
            {
                Icd10Code = d.Icd10Code,
                Description = d.Description,
                RecordedAt = d.RecordedAt,
                SourceEventId = d.SourceEventId
            }).ToList(),
        ActivePrescriptions = state.Prescriptions
            .Where(p => p.IsActive)
            .Select(p => new PrescriptionDto
            {
                MedicationName = p.MedicationName,
                Dosage = p.Dosage,
                Instructions = p.Instructions,
                PrescribedAt = p.PrescribedAt
            }).ToList(),
        Allergies = state.Allergies
            .Where(a => a.IsActive)
            .Select(a => new AllergyDto { Allergen = a.Allergen, Severity = a.Severity }).ToList(),
        Immunizations = state.Immunizations
            .Select(i => new ImmunizationDto { VaccineName = i.VaccineName, AdministeredAt = i.AdministeredAt }).ToList(),
        LatestVital = state.LatestVital is null ? null : new VitalSignDto
        {
            VitalType = state.LatestVital.VitalType,
            Value = state.LatestVital.Value,
            Unit = state.LatestVital.Unit,
            RecordedAt = state.LatestVital.RecordedAt
        },
        RecentLabResults = state.RecentLabResults
            .Select(l => new LabResultDto
            {
                TestName = l.TestName,
                ResultValue = l.ResultValue,
                IsCritical = l.IsCritical,
                ReceivedAt = l.ReceivedAt
            }).ToList()
    };
}
