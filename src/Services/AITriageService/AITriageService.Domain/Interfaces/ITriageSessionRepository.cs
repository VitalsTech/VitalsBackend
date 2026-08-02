using AITriageService.Domain.Entities;

namespace AITriageService.Domain.Interfaces;

public interface ITriageSessionRepository
{
    Task<TriageSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TriageSession>> GetByPatientIdAsync(Guid patientId, int limit, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TriageSession>> GetByPatientIdsAsync(
        IReadOnlyList<Guid> patientIds,
        int limit,
        CancellationToken cancellationToken = default);
    Task AddAsync(TriageSession session, CancellationToken cancellationToken = default);
    Task AddMessageAsync(TriageMessage message, CancellationToken cancellationToken = default);
    Task AddAssessmentAsync(TriageAssessment assessment, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
