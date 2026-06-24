using AITriageService.Domain.Entities;
using AITriageService.Domain.Interfaces;
using AITriageService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AITriageService.Infrastructure.Repositories;

public sealed class TriageSessionRepository : ITriageSessionRepository
{
    private readonly TriageDbContext _db;

    public TriageSessionRepository(TriageDbContext db) => _db = db;

    public Task<TriageSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
        _db.Sessions
            .Include(s => s.Messages)
            .Include(s => s.Assessments)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

    public async Task AddAsync(TriageSession session, CancellationToken cancellationToken = default) =>
        await _db.Sessions.AddAsync(session, cancellationToken);

    public async Task AddMessageAsync(TriageMessage message, CancellationToken cancellationToken = default) =>
        await _db.Messages.AddAsync(message, cancellationToken);

    public async Task AddAssessmentAsync(TriageAssessment assessment, CancellationToken cancellationToken = default) =>
        await _db.Assessments.AddAsync(assessment, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
