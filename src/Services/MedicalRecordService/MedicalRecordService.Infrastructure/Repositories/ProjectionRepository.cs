using MedicalRecordService.Domain.Entities;
using MedicalRecordService.Domain.Interfaces;
using MedicalRecordService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MedicalRecordService.Infrastructure.Repositories;

public sealed class ProjectionRepository : IProjectionRepository
{
    private readonly MedicalRecordDbContext _db;

    public ProjectionRepository(MedicalRecordDbContext db) => _db = db;

    public async Task<PatientStateProjections> GetCurrentStateAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var diagnoses = await _db.Diagnoses.Where(x => x.PatientId == patientId).ToListAsync(cancellationToken);
        var prescriptions = await _db.Prescriptions.Where(x => x.PatientId == patientId).ToListAsync(cancellationToken);
        var allergies = await _db.Allergies.Where(x => x.PatientId == patientId).ToListAsync(cancellationToken);
        var immunizations = await _db.Immunizations.Where(x => x.PatientId == patientId).ToListAsync(cancellationToken);
        var latestVital = await _db.VitalSigns
            .Where(x => x.PatientId == patientId)
            .OrderByDescending(x => x.RecordedAt)
            .FirstOrDefaultAsync(cancellationToken);
        var labs = await _db.LabResults
            .Where(x => x.PatientId == patientId)
            .OrderByDescending(x => x.ReceivedAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        return new PatientStateProjections
        {
            Diagnoses = diagnoses,
            Prescriptions = prescriptions,
            Allergies = allergies,
            Immunizations = immunizations,
            LatestVital = latestVital,
            RecentLabResults = labs
        };
    }

    public async Task ApplyDiagnosisAsync(PatientDiagnosisProjection item, CancellationToken cancellationToken = default) =>
        await _db.Diagnoses.AddAsync(item, cancellationToken);

    public async Task SupersedeDiagnosisAsync(Guid patientId, Guid supersededByEventId, string? icd10Code, CancellationToken cancellationToken = default)
    {
        var query = _db.Diagnoses.Where(x => x.PatientId == patientId && x.IsActive);
        if (!string.IsNullOrWhiteSpace(icd10Code))
            query = query.Where(x => x.Icd10Code == icd10Code);

        var active = await query.ToListAsync(cancellationToken);
        foreach (var d in active)
        {
            d.IsActive = false;
            d.SupersededByEventId = supersededByEventId;
        }
    }

    public async Task ApplyPrescriptionAsync(PatientPrescriptionProjection item, CancellationToken cancellationToken = default) =>
        await _db.Prescriptions.AddAsync(item, cancellationToken);

    public async Task RevokePrescriptionAsync(Guid patientId, Guid sourceEventId, CancellationToken cancellationToken = default)
    {
        var item = await _db.Prescriptions.FirstOrDefaultAsync(
            x => x.PatientId == patientId && x.SourceEventId == sourceEventId, cancellationToken);
        if (item is not null)
            item.IsActive = false;
    }

    public async Task ApplyAllergyAsync(PatientAllergyProjection item, CancellationToken cancellationToken = default) =>
        await _db.Allergies.AddAsync(item, cancellationToken);

    public async Task ApplyImmunizationAsync(PatientImmunizationProjection item, CancellationToken cancellationToken = default) =>
        await _db.Immunizations.AddAsync(item, cancellationToken);

    public async Task ApplyVitalSignAsync(PatientVitalSignProjection item, CancellationToken cancellationToken = default) =>
        await _db.VitalSigns.AddAsync(item, cancellationToken);

    public async Task ApplyLabResultAsync(PatientLabResultProjection item, CancellationToken cancellationToken = default) =>
        await _db.LabResults.AddAsync(item, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
