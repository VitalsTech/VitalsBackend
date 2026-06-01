using MedicalRecordService.Domain.Entities;
using MedicalRecordService.Domain.Interfaces;
using MedicalRecordService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MedicalRecordService.Infrastructure.Repositories;

public sealed class PatientSnapshotRepository : IPatientSnapshotRepository
{
    private readonly MedicalRecordDbContext _db;

    public PatientSnapshotRepository(MedicalRecordDbContext db) => _db = db;

    public Task<PatientSnapshot?> GetLatestAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        _db.PatientSnapshots
            .Where(x => x.PatientId == patientId)
            .OrderByDescending(x => x.UpToVersion)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(PatientSnapshot snapshot, CancellationToken cancellationToken = default) =>
        await _db.PatientSnapshots.AddAsync(snapshot, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}

public sealed class AccessGrantRepository : IAccessGrantRepository
{
    private readonly MedicalRecordDbContext _db;

    public AccessGrantRepository(MedicalRecordDbContext db) => _db = db;

    public async Task<IReadOnlyList<AccessGrant>> GetActiveGrantsAsync(
        Guid patientId,
        Guid granteeId,
        CancellationToken cancellationToken = default) =>
        await _db.AccessGrants
            .Where(x => x.PatientId == patientId &&
                        x.GranteeId == granteeId &&
                        x.RevokedAt == null &&
                        (x.ExpiresAt == null || x.ExpiresAt > DateTime.UtcNow))
            .ToListAsync(cancellationToken);

    public Task<AccessGrant?> GetByIdAsync(Guid grantId, CancellationToken cancellationToken = default) =>
        _db.AccessGrants.FirstOrDefaultAsync(x => x.Id == grantId, cancellationToken);

    public async Task AddAsync(AccessGrant grant, CancellationToken cancellationToken = default) =>
        await _db.AccessGrants.AddAsync(grant, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly MedicalRecordDbContext _db;

    public AuditLogRepository(MedicalRecordDbContext db) => _db = db;

    public async Task AddAsync(AuditLogEntry entry, CancellationToken cancellationToken = default) =>
        await _db.AuditLogs.AddAsync(entry, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
