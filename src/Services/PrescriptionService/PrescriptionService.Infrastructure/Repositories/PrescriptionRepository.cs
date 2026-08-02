using PrescriptionService.Application.Interfaces;
using PrescriptionService.Domain.Entities;
using PrescriptionService.Domain.Enums;
using PrescriptionService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace PrescriptionService.Infrastructure.Repositories;

public sealed class PrescriptionRepository : IPrescriptionRepository
{
    private readonly PrescriptionDbContext _db;

    public PrescriptionRepository(PrescriptionDbContext db) => _db = db;

    public Task<Prescription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Prescriptions.AsNoTracking().Include(x => x.Medications).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Prescription?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Prescriptions.Include(x => x.Medications).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Prescription>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        await _db.Prescriptions.AsNoTracking().Include(x => x.Medications)
            .Where(x => x.PatientId == patientId)
            .OrderByDescending(x => x.IssuedAt)
            .ToListAsync(cancellationToken);

    public async Task SaveAsync(Prescription prescription, CancellationToken cancellationToken = default)
    {
        if (_db.Entry(prescription).State == EntityState.Detached)
            _db.Prescriptions.Add(prescription);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddStatusHistoryAsync(PrescriptionStatusHistory history, CancellationToken cancellationToken = default)
    {
        _db.PrescriptionStatusHistory.Add(history);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Prescription>> GetExpiringSoonAsync(DateTime thresholdDate, CancellationToken cancellationToken = default) =>
        await _db.Prescriptions
            .Where(x => (x.Status == PrescriptionStatus.Signed || x.Status == PrescriptionStatus.SentToPharmacy)
                        && x.ValidUntil.Date == thresholdDate.Date)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Prescription>> GetExpiredCandidatesAsync(DateTime utcNow, CancellationToken cancellationToken = default) =>
        await _db.Prescriptions
            .Where(x => (x.Status == PrescriptionStatus.Signed || x.Status == PrescriptionStatus.SentToPharmacy)
                        && x.ValidUntil < utcNow)
            .ToListAsync(cancellationToken);
}
