using MedicalRecordService.Domain.Entities;
using MedicalRecordService.Domain.Interfaces;
using MedicalRecordService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MedicalRecordService.Infrastructure.Repositories;

public sealed class MedicalEventRepository : IMedicalEventRepository
{
    private readonly MedicalRecordDbContext _db;

    public MedicalEventRepository(MedicalRecordDbContext db) => _db = db;

    public Task<MedicalEvent?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default) =>
        _db.MedicalEvents.FirstOrDefaultAsync(x => x.EventId == eventId, cancellationToken);

    public async Task<long> GetLatestVersionAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var max = await _db.MedicalEvents
            .Where(x => x.PatientId == patientId)
            .MaxAsync(x => (long?)x.Version, cancellationToken);
        return max ?? 0;
    }

    public async Task<IReadOnlyList<MedicalEvent>> GetEventsAfterVersionAsync(Guid patientId, long afterVersion, CancellationToken cancellationToken = default) =>
        await _db.MedicalEvents
            .Where(x => x.PatientId == patientId && x.Version > afterVersion)
            .OrderBy(x => x.Version)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<MedicalEvent>> GetEventsAsync(
        Guid patientId,
        DateTime? from,
        DateTime? to,
        IReadOnlyList<string>? eventTypes,
        CancellationToken cancellationToken = default)
    {
        var query = _db.MedicalEvents.Where(x => x.PatientId == patientId);
        if (from.HasValue)
            query = query.Where(x => x.OccurredAt >= from.Value);
        if (to.HasValue)
            query = query.Where(x => x.OccurredAt <= to.Value);
        if (eventTypes is { Count: > 0 })
            query = query.Where(x => eventTypes.Contains(x.EventType));

        return await query.OrderBy(x => x.Version).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(MedicalEvent medicalEvent, CancellationToken cancellationToken = default) =>
        await _db.MedicalEvents.AddAsync(medicalEvent, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
