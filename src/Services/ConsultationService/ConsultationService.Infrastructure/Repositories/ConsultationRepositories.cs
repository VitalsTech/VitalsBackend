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

    public async Task AddParticipantAsync(SessionParticipant participant, CancellationToken cancellationToken = default)
    {
        var exists = await _db.Participants.AnyAsync(
            x => x.SessionId == participant.SessionId && x.UserId == participant.UserId,
            cancellationToken);

        if (!exists)
        {
            _db.Participants.Add(participant);
            await _db.SaveChangesAsync(cancellationToken);
        }
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

    public async Task<ConsultationMessage> AddMessageAsync(ConsultationMessage message, CancellationToken cancellationToken = default)
    {
        _db.Messages.Add(message);
        await _db.SaveChangesAsync(cancellationToken);
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
