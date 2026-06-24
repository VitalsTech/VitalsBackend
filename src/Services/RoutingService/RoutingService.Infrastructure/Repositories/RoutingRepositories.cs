using RoutingService.Application.Interfaces;
using RoutingService.Domain.Entities;
using RoutingService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace RoutingService.Infrastructure.Repositories;

public sealed class RoutingDecisionRepository : IRoutingDecisionRepository
{
    private readonly RoutingDbContext _db;

    public RoutingDecisionRepository(RoutingDbContext db)
    {
        _db = db;
    }

    public Task<RoutingDecision?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.RoutingDecisions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task SaveDecisionWithAuditAsync(RoutingDecision decision, RoutingAuditEntry audit, CancellationToken cancellationToken = default)
    {
        _db.RoutingDecisions.Add(decision);
        _db.RoutingAuditEntries.Add(audit);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ClinicRoutingRule>> GetActiveRulesAsync(string clinicId, CancellationToken cancellationToken = default) =>
        await _db.ClinicRoutingRules
            .AsNoTracking()
            .Where(x => x.ClinicId == clinicId && x.IsActive)
            .ToListAsync(cancellationToken);
}

public sealed class PatientRouteRepository : IPatientRouteRepository
{
    private readonly RoutingDbContext _db;

    public PatientRouteRepository(RoutingDbContext db)
    {
        _db = db;
    }

    public Task<PatientRoute?> GetActiveByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        _db.PatientRoutes.FirstOrDefaultAsync(x => x.PatientId == patientId && x.Status == "active", cancellationToken);

    public async Task SaveAsync(PatientRoute route, CancellationToken cancellationToken = default)
    {
        if (_db.Entry(route).State == EntityState.Detached)
            _db.PatientRoutes.Add(route);

        await _db.SaveChangesAsync(cancellationToken);
    }
}
