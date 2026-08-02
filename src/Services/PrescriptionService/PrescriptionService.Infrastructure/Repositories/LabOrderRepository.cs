using Microsoft.EntityFrameworkCore;
using PrescriptionService.Application.Interfaces;
using PrescriptionService.Domain.Entities;
using PrescriptionService.Infrastructure.Data;

namespace PrescriptionService.Infrastructure.Repositories;

public sealed class LabOrderRepository : ILabOrderRepository
{
    private readonly PrescriptionDbContext _db;

    public LabOrderRepository(PrescriptionDbContext db) => _db = db;

    public Task<LabOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.LabOrders.AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<LabOrder?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.LabOrders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<LabOrder>> GetByPatientIdAsync(
        Guid patientId,
        CancellationToken cancellationToken = default) =>
        await _db.LabOrders.AsNoTracking()
            .Include(x => x.Items)
            .Where(x => x.PatientId == patientId)
            .OrderByDescending(x => x.OrderedAt)
            .ToListAsync(cancellationToken);

    public async Task SaveAsync(LabOrder order, CancellationToken cancellationToken = default)
    {
        if (_db.Entry(order).State == EntityState.Detached)
            _db.LabOrders.Add(order);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddStatusHistoryAsync(LabOrderStatusHistory history, CancellationToken cancellationToken = default)
    {
        _db.LabOrderStatusHistory.Add(history);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
