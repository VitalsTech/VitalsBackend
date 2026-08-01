using ConsultationService.Application.Interfaces;
using ConsultationService.Domain.Entities;
using ConsultationService.Domain.Enums;
using ConsultationService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ConsultationService.Infrastructure.Repositories;

public sealed class ConsultationRepository : IConsultationRepository
{
    private readonly ConsultationDbContext _db;

    public ConsultationRepository(ConsultationDbContext db) => _db = db;

    public Task<ConsultationSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
        _db.Sessions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == sessionId, cancellationToken);

    public Task<ConsultationSession?> GetByIdForUpdateAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
        _db.Sessions.FirstOrDefaultAsync(x => x.Id == sessionId, cancellationToken);

    public async Task<ConsultationSession?> FindActiveBetweenAsync(
        Guid patientId,
        Guid doctorId,
        IReadOnlyList<Guid>? alternateDoctorIds = null,
        IReadOnlyList<Guid>? alternatePatientIds = null,
        CancellationToken cancellationToken = default)
    {
        var doctorIds = new HashSet<Guid> { doctorId };
        if (alternateDoctorIds is not null)
            foreach (var id in alternateDoctorIds)
                if (id != Guid.Empty) doctorIds.Add(id);

        var patientIds = new HashSet<Guid> { patientId };
        if (alternatePatientIds is not null)
            foreach (var id in alternatePatientIds)
                if (id != Guid.Empty) patientIds.Add(id);

        var terminal = new[]
        {
            ConsultationStatus.Completed,
            ConsultationStatus.Cancelled,
            ConsultationStatus.Expired
        };

        return await _db.Sessions
            .AsNoTracking()
            .Where(s => patientIds.Contains(s.PatientId) && doctorIds.Contains(s.DoctorId))
            .Where(s => !terminal.Contains(s.Status))
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ConsultationSession?> FindLatestByPatientAsync(
        Guid patientId,
        CancellationToken cancellationToken = default) =>
        await _db.Sessions
            .AsNoTracking()
            .Where(s => s.PatientId == patientId)
            .OrderByDescending(s => s.LastActivityAt)
            .ThenByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<ConsultationSession>> GetByDoctorInRangeAsync(
        IReadOnlyList<Guid> doctorIds,
        DateTime from,
        DateTime to,
        DateTime? openSince = null,
        CancellationToken cancellationToken = default)
    {
        var ids = doctorIds.Where(id => id != Guid.Empty).Distinct().ToArray();
        if (ids.Length == 0)
            return Array.Empty<ConsultationSession>();

        var terminal = new[]
        {
            ConsultationStatus.Completed,
            ConsultationStatus.Cancelled,
            ConsultationStatus.Expired
        };

        var scheduled = await _db.Sessions
            .AsNoTracking()
            .Where(s => ids.Contains(s.DoctorId))
            .Where(s => (s.ScheduledAt ?? s.StartedAt ?? s.CreatedAt) >= from
                && (s.ScheduledAt ?? s.StartedAt ?? s.CreatedAt) < to)
            .ToListAsync(cancellationToken);

        var openQuery = _db.Sessions
            .AsNoTracking()
            .Where(s => ids.Contains(s.DoctorId) && !terminal.Contains(s.Status));

        if (openSince.HasValue)
            openQuery = openQuery.Where(s => s.LastActivityAt >= openSince.Value);

        var open = await openQuery.ToListAsync(cancellationToken);

        return scheduled
            .Concat(open)
            .GroupBy(s => s.Id)
            .Select(g => g.First())
            .OrderBy(s => s.ScheduledAt ?? s.StartedAt ?? s.CreatedAt)
            .ToList();
    }

    public async Task<IReadOnlyList<ConsultationSession>> ListForParticipantAsync(
        IReadOnlyList<Guid> identityIds,
        bool asPatient,
        bool asDoctor,
        bool includeCompleted,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var ids = identityIds.Where(id => id != Guid.Empty).Distinct().ToArray();
        if (ids.Length == 0 || (!asPatient && !asDoctor))
            return Array.Empty<ConsultationSession>();

        var take = Math.Clamp(limit, 1, 100);
        var terminal = new[]
        {
            ConsultationStatus.Completed,
            ConsultationStatus.Cancelled,
            ConsultationStatus.Expired
        };

        var query = _db.Sessions.AsNoTracking().AsQueryable();

        if (asPatient && asDoctor)
            query = query.Where(s => ids.Contains(s.PatientId) || ids.Contains(s.DoctorId));
        else if (asPatient)
            query = query.Where(s => ids.Contains(s.PatientId));
        else
            query = query.Where(s => ids.Contains(s.DoctorId));

        if (!includeCompleted)
            query = query.Where(s => !terminal.Contains(s.Status));

        return await query
            .OrderByDescending(s => s.ScheduledAt ?? s.StartedAt ?? s.CreatedAt)
            .ThenByDescending(s => s.LastActivityAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveSessionAsync(ConsultationSession session, CancellationToken cancellationToken = default)
    {
        if (_db.Entry(session).State == EntityState.Detached)
            _db.Sessions.Add(session);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddTransitionAsync(SessionStatusTransition transition, CancellationToken cancellationToken = default)
    {
        _db.StatusTransitions.Add(transition);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> AddParticipantAsync(SessionParticipant participant, CancellationToken cancellationToken = default)
    {
        var exists = await _db.Participants.AnyAsync(
            x => x.SessionId == participant.SessionId && x.UserId == participant.UserId,
            cancellationToken);

        if (exists)
            return false;

        _db.Participants.Add(participant);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<ConsultationSession>> GetExpiredCandidatesAsync(DateTime utcNow, CancellationToken cancellationToken = default) =>
        await _db.Sessions
            .Where(x => x.Status == ConsultationStatus.Created || x.Status == ConsultationStatus.PatientJoined)
            .Where(x => x.LastActivityAt < utcNow.AddMinutes(-30))
            .ToListAsync(cancellationToken);
}

public sealed class MessageRepository : IMessageRepository
{
    private readonly ConsultationDbContext _db;

    public MessageRepository(ConsultationDbContext db) => _db = db;

    public async Task<ConsultationMessage> AddMessageAsync(
        ConsultationSession session,
        ConsultationMessage message,
        CancellationToken cancellationToken = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        // Hold the session row until sequence is allocated and message is inserted.
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"""SELECT 1 FROM consultation_sessions WHERE "Id" = {session.Id} FOR UPDATE""",
            cancellationToken);

        var maxInDb = await _db.Messages
            .Where(m => m.SessionId == session.Id)
            .Select(m => (long?)m.SequenceNumber)
            .MaxAsync(cancellationToken) ?? 0;

        // Re-read counter from DB in case tracked entity is stale.
        var dbCounter = await _db.Sessions
            .AsNoTracking()
            .Where(s => s.Id == session.Id)
            .Select(s => s.LastSequenceNumber)
            .FirstAsync(cancellationToken);

        var next = Math.Max(Math.Max(session.LastSequenceNumber, dbCounter), maxInDb) + 1;
        message.SequenceNumber = next;
        message.SessionId = session.Id;

        session.LastSequenceNumber = next;
        session.LastMessageId = message.Id;
        session.LastActivityAt = DateTime.UtcNow;

        _db.Messages.Add(message);

        if (_db.Entry(session).State == EntityState.Detached)
            _db.Sessions.Attach(session);

        _db.Entry(session).Property(x => x.LastSequenceNumber).IsModified = true;
        _db.Entry(session).Property(x => x.LastMessageId).IsModified = true;
        _db.Entry(session).Property(x => x.LastActivityAt).IsModified = true;

        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return message;
    }

    public async Task<IReadOnlyList<ConsultationMessage>> GetMessagesAfterSequenceAsync(
        Guid sessionId,
        long afterSequence,
        CancellationToken cancellationToken = default) =>
        await _db.Messages
            .AsNoTracking()
            .Where(x => x.SessionId == sessionId && x.SequenceNumber > afterSequence)
            .OrderBy(x => x.SequenceNumber)
            .ToListAsync(cancellationToken);

    public async Task MarkReadAsync(Guid sessionId, ParticipantRole readerRole, CancellationToken cancellationToken = default)
    {
        var senderRole = readerRole == ParticipantRole.Patient ? ParticipantRole.Doctor : ParticipantRole.Patient;
        var unread = await _db.Messages
            .Where(x => x.SessionId == sessionId && x.SenderRole == senderRole && x.ReadAt == null)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var msg in unread)
            msg.ReadAt = now;

        var session = await _db.Sessions.FirstOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
        if (session is not null)
        {
            if (readerRole == ParticipantRole.Patient)
                session.PatientUnreadCount = 0;
            else
                session.DoctorUnreadCount = 0;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
