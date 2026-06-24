using System.Text.Json;
using ConsultationService.Application.DTOs;
using ConsultationService.Application.Interfaces;
using ConsultationService.Application.Options;
using ConsultationService.Application.Services;
using ConsultationService.Domain.Entities;
using ConsultationService.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vitals.ESignature;

namespace ConsultationService.Application.Services;

public sealed class ConsultationAppService : IConsultationService
{
    private readonly IConsultationRepository _sessions;
    private readonly IMessageRepository _messages;
    private readonly ISessionStateStore _stateStore;
    private readonly IMedicalRecordEventClient _medicalRecord;
    private readonly IConsultationEventPublisher _publisher;
    private readonly ISfuSignalingService _sfu;
    private readonly IConsultationChatNotifier _notifier;
    private readonly IESignatureProvider _signature;
    private readonly KafkaOptions _kafka;
    private readonly ConsultationOptions _options;
    private readonly ILogger<ConsultationAppService> _logger;

    public ConsultationAppService(
        IConsultationRepository sessions,
        IMessageRepository messages,
        ISessionStateStore stateStore,
        IMedicalRecordEventClient medicalRecord,
        IConsultationEventPublisher publisher,
        ISfuSignalingService sfu,
        IConsultationChatNotifier notifier,
        IESignatureProvider signature,
        IOptions<KafkaOptions> kafka,
        IOptions<ConsultationOptions> options,
        ILogger<ConsultationAppService> logger)
    {
        _sessions = sessions;
        _messages = messages;
        _stateStore = stateStore;
        _medicalRecord = medicalRecord;
        _publisher = publisher;
        _sfu = sfu;
        _notifier = notifier;
        _signature = signature;
        _kafka = kafka.Value;
        _options = options.Value;
        _logger = logger;
    }

    public Task<ConsultationSessionResponse> CreateFromRoutingDecisionAsync(
        RoutingDecisionEventDto decision,
        CancellationToken cancellationToken = default)
    {
        if (!decision.DoctorId.HasValue)
            throw new InvalidOperationException("Routing decision must include DoctorId for consultation.");

        return CreateSessionInternalAsync(
            decision.PatientId,
            decision.DoctorId.Value,
            decision.DoctorName,
            SessionLifecycle.ParseType(decision.ConsultationFormat),
            decision.EffectiveUrgencyLevel,
            decision.RoutingDecisionId ?? decision.SessionId,
            decision.SessionId,
            cancellationToken);
    }

    public Task<ConsultationSessionResponse> CreateManualAsync(
        CreateConsultationRequest request,
        CancellationToken cancellationToken = default) =>
        CreateSessionInternalAsync(
            request.PatientId,
            request.DoctorId,
            request.DoctorName,
            SessionLifecycle.ParseType(request.ConsultationType),
            request.UrgencyLevel,
            request.RoutingDecisionId,
            request.TriageSessionId,
            cancellationToken);

    public async Task<ConsultationSessionResponse> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _sessions.GetByIdAsync(sessionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Session {sessionId} not found.");
        return MapSession(session);
    }

    public async Task<ConsultationSessionResponse> JoinAsync(
        Guid sessionId,
        Guid userId,
        ParticipantRole role,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessions.GetByIdForUpdateAsync(sessionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Session {sessionId} not found.");

        EnsureParticipant(session, userId, role);
        EnsureNotTerminal(session);

        var targetStatus = role switch
        {
            ParticipantRole.Patient when session.Status is ConsultationStatus.Created or ConsultationStatus.DoctorJoined
                => session.Status == ConsultationStatus.DoctorJoined ? ConsultationStatus.Active : ConsultationStatus.PatientJoined,
            ParticipantRole.Doctor when session.Status is ConsultationStatus.Created or ConsultationStatus.PatientJoined
                => session.Status == ConsultationStatus.PatientJoined ? ConsultationStatus.Active : ConsultationStatus.DoctorJoined,
            ParticipantRole.Doctor when session.Status == ConsultationStatus.DoctorJoined
                => ConsultationStatus.Active,
            ParticipantRole.Patient when session.Status == ConsultationStatus.PatientJoined
                => ConsultationStatus.PatientJoined,
            _ => session.Status
        };

        if (targetStatus != session.Status)
            await TransitionAsync(session, targetStatus, role.ToString(), userId, null, cancellationToken);

        if (session.Status == ConsultationStatus.Active && session.StartedAt is null)
            session.StartedAt = DateTime.UtcNow;

        session.LastActivityAt = DateTime.UtcNow;
        await _sessions.AddParticipantAsync(new SessionParticipant
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            UserId = userId,
            Role = role,
            JoinedAt = DateTime.UtcNow
        }, cancellationToken);

        await _sessions.SaveSessionAsync(session, cancellationToken);
        await SyncStateStoreAsync(session, cancellationToken);

        var systemText = role == ParticipantRole.Doctor
            ? "Врач подключился к консультации."
            : "Пациент подключился к консультации.";
        await SendSystemMessageAsync(session, systemText, cancellationToken);

        await _medicalRecord.AppendConsultationEventAsync(session.PatientId, "ConsultationJoined", new
        {
            session.Id,
            userId,
            Role = role.ToString(),
            session.Status
        }, session.Id, cancellationToken);

        return MapSession(session);
    }

    public async Task<ConsultationSessionResponse> PauseAsync(Guid sessionId, Guid doctorId, CancellationToken cancellationToken = default)
    {
        var session = await RequireDoctorSessionAsync(sessionId, doctorId, cancellationToken);
        await TransitionAsync(session, ConsultationStatus.Paused, "doctor", doctorId, null, cancellationToken);
        session.PausedAt = DateTime.UtcNow;
        await _sessions.SaveSessionAsync(session, cancellationToken);
        await SendSystemMessageAsync(session, "Врач приостановил консультацию для уточнения данных.", cancellationToken);
        await _notifier.NotifyStatusChangedAsync(sessionId, session.Status.ToString(), cancellationToken);
        return MapSession(session);
    }

    public async Task<ConsultationSessionResponse> ResumeAsync(Guid sessionId, Guid doctorId, CancellationToken cancellationToken = default)
    {
        var session = await RequireDoctorSessionAsync(sessionId, doctorId, cancellationToken);
        if (session.Status != ConsultationStatus.Paused)
            return MapSession(session);

        await TransitionAsync(session, ConsultationStatus.Active, "doctor", doctorId, null, cancellationToken);
        session.PausedAt = null;
        await _sessions.SaveSessionAsync(session, cancellationToken);
        await SendSystemMessageAsync(session, "Консультация возобновлена.", cancellationToken);
        await _notifier.NotifyStatusChangedAsync(sessionId, session.Status.ToString(), cancellationToken);
        return MapSession(session);
    }

    public async Task<ConsultationSessionResponse> DoctorLeaveAsync(Guid sessionId, Guid doctorId, CancellationToken cancellationToken = default)
    {
        var session = await RequireDoctorSessionAsync(sessionId, doctorId, cancellationToken);
        await TransitionAsync(session, ConsultationStatus.DoctorLeft, "doctor", doctorId, null, cancellationToken);
        await _sessions.SaveSessionAsync(session, cancellationToken);
        await SendSystemMessageAsync(session, "Врач завершил консультацию. Подтвердите, что всё понятно.", cancellationToken);
        await _notifier.NotifyStatusChangedAsync(sessionId, session.Status.ToString(), cancellationToken);
        return MapSession(session);
    }

    public async Task<ConsultationSessionResponse> CompleteAsync(
        Guid sessionId,
        Guid doctorId,
        CompleteConsultationRequest request,
        CancellationToken cancellationToken = default)
    {
        var session = await RequireDoctorSessionAsync(sessionId, doctorId, cancellationToken);
        session.ProtocolJson = JsonSerializer.Serialize(request);

        var signResult = await _signature.SignAsync(new SignDocumentRequest
        {
            DocumentType = "consultation-protocol",
            DocumentId = sessionId,
            SignerId = doctorId,
            ContentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(session.ProtocolJson))
        }, cancellationToken).ConfigureAwait(false);
        session.ProtocolSignature = signResult.Signature;

        await TransitionAsync(session, ConsultationStatus.DoctorLeft, "doctor", doctorId, "Protocol submitted", cancellationToken);
        await _sessions.SaveSessionAsync(session, cancellationToken);

        await _medicalRecord.AppendConsultationEventAsync(session.PatientId, "ConsultationProtocolDraft", new
        {
            session.Id,
            request.Complaints,
            request.PreliminaryDiagnosisIcd10,
            request.PreliminaryDiagnosisText,
            request.Recommendations,
            request.Prescriptions,
            request.LabOrders,
            Signature = session.ProtocolSignature
        }, session.Id, cancellationToken);

        return MapSession(session);
    }

    public async Task<ConsultationSessionResponse> PatientConfirmAsync(Guid sessionId, Guid patientId, CancellationToken cancellationToken = default)
    {
        var session = await _sessions.GetByIdForUpdateAsync(sessionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Session {sessionId} not found.");

        if (session.PatientId != patientId)
            throw new UnauthorizedAccessException("Only the patient can confirm completion.");

        if (session.Status is not (ConsultationStatus.DoctorLeft or ConsultationStatus.Active))
            throw new InvalidOperationException($"Cannot confirm from status {session.Status}.");

        await TransitionAsync(session, ConsultationStatus.Completed, "patient", patientId, null, cancellationToken);
        session.CompletedAt = DateTime.UtcNow;
        await _sessions.SaveSessionAsync(session, cancellationToken);

        var protocol = JsonSerializer.Deserialize<CompleteConsultationRequest>(session.ProtocolJson);
        await _medicalRecord.AppendConsultationEventAsync(session.PatientId, "ConsultationCompleted", new
        {
            session.Id,
            session.StartedAt,
            session.CompletedAt,
            Protocol = protocol,
            Signature = session.ProtocolSignature
        }, session.Id, cancellationToken);

        await _publisher.PublishAsync(_kafka.ConsultationCompletedTopic, new
        {
            session.Id,
            session.PatientId,
            session.DoctorId,
            session.CompletedAt,
            Protocol = protocol
        }, cancellationToken);

        await _notifier.NotifyStatusChangedAsync(sessionId, session.Status.ToString(), cancellationToken);
        return MapSession(session);
    }

    public async Task<ConsultationSessionResponse> CancelAsync(
        Guid sessionId,
        Guid userId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessions.GetByIdForUpdateAsync(sessionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Session {sessionId} not found.");

        if (userId != session.PatientId && userId != session.DoctorId)
            throw new UnauthorizedAccessException("Not a session participant.");

        EnsureNotTerminal(session);
        session.CancelReason = reason;
        await TransitionAsync(session, ConsultationStatus.Cancelled, "user", userId, reason, cancellationToken);
        await _sessions.SaveSessionAsync(session, cancellationToken);
        await SendSystemMessageAsync(session, $"Консультация отменена: {reason}", cancellationToken);

        await _medicalRecord.AppendConsultationEventAsync(session.PatientId, "ConsultationCancelled", new
        {
            session.Id,
            reason
        }, session.Id, cancellationToken);

        return MapSession(session);
    }

    public async Task<ConsultationSessionResponse> RecordConsentAsync(
        Guid sessionId,
        Guid patientId,
        ConsentRequest request,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessions.GetByIdForUpdateAsync(sessionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Session {sessionId} not found.");

        if (session.PatientId != patientId)
            throw new UnauthorizedAccessException("Only the patient can record consent.");

        if (!request.DataProcessingConsent)
            throw new InvalidOperationException("Data processing consent is required.");

        session.PatientConsentGiven = true;
        session.PatientConsentAt = DateTime.UtcNow;
        session.VideoRecordingConsent = request.VideoRecordingConsent;
        session.LastActivityAt = DateTime.UtcNow;
        await _sessions.SaveSessionAsync(session, cancellationToken);

        await _medicalRecord.AppendConsultationEventAsync(session.PatientId, "ConsultationConsent", new
        {
            session.Id,
            request.DataProcessingConsent,
            request.VideoRecordingConsent,
            session.PatientConsentAt
        }, session.Id, cancellationToken);

        return MapSession(session);
    }

    public async Task<ConsultationMessageDto> SendMessageAsync(
        Guid sessionId,
        Guid senderId,
        ParticipantRole role,
        SendMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessions.GetByIdForUpdateAsync(sessionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Session {sessionId} not found.");

        EnsureNotTerminal(session);

        if (!Enum.TryParse<MessageType>(request.MessageType, true, out var messageType))
            messageType = MessageType.Text;

        if (role == ParticipantRole.Patient && !session.PatientConsentGiven && messageType != MessageType.System)
            throw new InvalidOperationException("Patient must accept consent before messaging.");

        var sequence = session.LastSequenceNumber + 1;
        var message = new ConsultationMessage
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            SequenceNumber = sequence,
            SenderId = senderId,
            SenderRole = role,
            MessageType = messageType,
            Content = request.Content,
            AttachmentUrl = request.AttachmentUrl,
            IsImportant = request.IsImportant,
            SentAt = DateTime.UtcNow
        };

        await _messages.AddMessageAsync(message, cancellationToken);

        session.LastSequenceNumber = sequence;
        session.LastMessageId = message.Id;
        session.LastActivityAt = DateTime.UtcNow;

        if (role == ParticipantRole.Patient)
            session.DoctorUnreadCount++;
        else
            session.PatientUnreadCount++;

        if (session.Status is ConsultationStatus.DoctorJoined or ConsultationStatus.PatientJoined)
            await TransitionAsync(session, ConsultationStatus.Active, role.ToString(), senderId, "First message", cancellationToken);

        if (session.StartedAt is null && session.Status == ConsultationStatus.Active)
            session.StartedAt = DateTime.UtcNow;

        await _sessions.SaveSessionAsync(session, cancellationToken);
        await SyncStateStoreAsync(session, cancellationToken);

        var dto = MapMessage(message);
        await _notifier.NotifyMessageAsync(sessionId, dto, cancellationToken);

        await _medicalRecord.AppendConsultationEventAsync(session.PatientId, "ConsultationMessage", new
        {
            sessionId,
            message.Id,
            message.SequenceNumber,
            message.MessageType,
            message.Content,
            message.AttachmentUrl,
            message.SentAt
        }, sessionId, cancellationToken);

        return dto;
    }

    public async Task<IReadOnlyList<ConsultationMessageDto>> GetMessagesAsync(
        Guid sessionId,
        long afterSequence,
        CancellationToken cancellationToken = default)
    {
        var messages = await _messages.GetMessagesAfterSequenceAsync(sessionId, afterSequence, cancellationToken);
        return messages.Select(MapMessage).ToList();
    }

    public async Task<VideoRoomResponse> StartVideoAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken = default)
    {
        var session = await _sessions.GetByIdForUpdateAsync(sessionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Session {sessionId} not found.");

        if (userId != session.PatientId && userId != session.DoctorId)
            throw new UnauthorizedAccessException("Not a session participant.");

        EnsureNotTerminal(session);

        if (string.IsNullOrEmpty(session.VideoRoomId))
        {
            var room = await _sfu.CreateRoomAsync(sessionId, cancellationToken);
            session.VideoRoomId = room.RoomId;
            session.Type = ConsultationType.Video;
            await _sessions.SaveSessionAsync(session, cancellationToken);
            await SendSystemMessageAsync(session, "Видеоконсультация начата.", cancellationToken);
            return room;
        }

        return await _sfu.CreateRoomAsync(sessionId, cancellationToken);
    }

    public async Task TriggerEmergencyAsync(Guid sessionId, Guid doctorId, CancellationToken cancellationToken = default)
    {
        var session = await RequireDoctorSessionAsync(sessionId, doctorId, cancellationToken);

        await _publisher.PublishAsync(_kafka.ConsultationEmergencyTopic, new
        {
            session.Id,
            session.PatientId,
            session.DoctorId,
            TriggeredAt = DateTime.UtcNow
        }, cancellationToken);

        await SendSystemMessageAsync(session, "Врач инициировал экстренный протокол. Данные переданы диспетчеру.", cancellationToken);
        await _medicalRecord.AppendConsultationEventAsync(session.PatientId, "ConsultationEmergency", new
        {
            session.Id,
            session.DoctorId
        }, session.Id, cancellationToken);
    }

    public async Task SubmitRatingAsync(Guid sessionId, Guid userId, SubmitRatingRequest request, CancellationToken cancellationToken = default)
    {
        var session = await _sessions.GetByIdForUpdateAsync(sessionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Session {sessionId} not found.");

        if (session.Status != ConsultationStatus.Completed)
            throw new InvalidOperationException("Ratings are allowed only after completion.");

        if (request.Role.Equals("patient", StringComparison.OrdinalIgnoreCase))
        {
            if (session.PatientId != userId)
                throw new UnauthorizedAccessException();
            session.PatientRating = request.Score;
            session.PatientFeedback = request.Feedback;
        }
        else if (request.Role.Equals("doctor", StringComparison.OrdinalIgnoreCase))
        {
            if (session.DoctorId != userId)
                throw new UnauthorizedAccessException();
            session.DoctorRating = request.Score;
            session.DoctorFeedback = request.Feedback;
        }
        else
        {
            throw new InvalidOperationException("Role must be patient or doctor.");
        }

        await _sessions.SaveSessionAsync(session, cancellationToken);
        await _publisher.PublishAsync("consultation.rated", request, cancellationToken);
    }

    public async Task ExpireAsync(Guid sessionId, string reason, CancellationToken cancellationToken = default)
    {
        var session = await _sessions.GetByIdForUpdateAsync(sessionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Session {sessionId} not found.");

        if (session.Status is ConsultationStatus.Completed or ConsultationStatus.Cancelled or ConsultationStatus.Expired)
            return;

        await SendSystemMessageAsync(session, $"Консультация просрочена: {reason}", cancellationToken);
        await TransitionAsync(session, ConsultationStatus.Expired, "system", null, reason, cancellationToken);
        await _sessions.SaveSessionAsync(session, cancellationToken);

        await _medicalRecord.AppendConsultationEventAsync(session.PatientId, "ConsultationExpired", new
        {
            session.Id,
            reason
        }, session.Id, cancellationToken);

        await _publisher.PublishAsync("consultation.expired", new { session.Id, reason }, cancellationToken);
    }

    public async Task<ConsultationSessionResponse> InviteDoctorAsync(
        Guid sessionId,
        Guid initiatorDoctorId,
        InviteDoctorRequest request,
        CancellationToken cancellationToken = default)
    {
        var session = await RequireDoctorSessionAsync(sessionId, initiatorDoctorId, cancellationToken);
        await _sessions.AddParticipantAsync(new SessionParticipant
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            UserId = request.DoctorId,
            Role = ParticipantRole.Doctor,
            JoinedAt = DateTime.UtcNow
        }, cancellationToken);

        await SendSystemMessageAsync(session, $"К консультации приглашён врач {request.DoctorName ?? request.DoctorId.ToString()}.", cancellationToken);
        return MapSession(session);
    }

    private async Task<ConsultationSessionResponse> CreateSessionInternalAsync(
        Guid patientId,
        Guid doctorId,
        string? doctorName,
        ConsultationType type,
        int urgencyLevel,
        Guid? routingDecisionId,
        Guid? triageSessionId,
        CancellationToken cancellationToken)
    {
        var session = new ConsultationSession
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            DoctorId = doctorId,
            DoctorName = doctorName,
            Type = type,
            Status = ConsultationStatus.Created,
            UrgencyLevel = urgencyLevel,
            ExpectedDurationMinutes = SessionLifecycle.DefaultDurationMinutes(type, _options),
            RoutingDecisionId = routingDecisionId,
            TriageSessionId = triageSessionId,
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow
        };

        await _sessions.SaveSessionAsync(session, cancellationToken);
        await _sessions.AddTransitionAsync(new SessionStatusTransition
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            FromStatus = ConsultationStatus.Created,
            ToStatus = ConsultationStatus.Created,
            Initiator = "system",
            OccurredAt = DateTime.UtcNow
        }, cancellationToken);

        await SyncStateStoreAsync(session, cancellationToken);

        await _medicalRecord.AppendConsultationEventAsync(patientId, "ConsultationStarted", new
        {
            session.Id,
            session.DoctorId,
            session.Type,
            session.UrgencyLevel,
            session.CreatedAt
        }, session.Id, cancellationToken);

        await _publisher.PublishAsync(_kafka.ConsultationCreatedTopic, new
        {
            session.Id,
            session.PatientId,
            session.DoctorId,
            session.Type,
            session.UrgencyLevel,
            session.RoutingDecisionId
        }, cancellationToken);

        _logger.LogInformation("Created consultation session {SessionId} for patient {PatientId}", session.Id, patientId);
        return MapSession(session);
    }

    private async Task TransitionAsync(
        ConsultationSession session,
        ConsultationStatus to,
        string initiator,
        Guid? userId,
        string? reason,
        CancellationToken cancellationToken)
    {
        if (!SessionLifecycle.CanTransition(session.Status, to))
            throw new InvalidOperationException($"Invalid transition {session.Status} -> {to}.");

        var from = session.Status;
        session.Status = to;
        session.LastActivityAt = DateTime.UtcNow;

        await _sessions.AddTransitionAsync(new SessionStatusTransition
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            FromStatus = from,
            ToStatus = to,
            Initiator = initiator,
            InitiatorUserId = userId,
            Reason = reason,
            OccurredAt = DateTime.UtcNow
        }, cancellationToken);

        await SyncStateStoreAsync(session, cancellationToken);
    }

    private async Task SendSystemMessageAsync(ConsultationSession session, string text, CancellationToken cancellationToken)
    {
        await SendMessageAsync(session.Id, Guid.Empty, ParticipantRole.Doctor, new SendMessageRequest
        {
            MessageType = nameof(MessageType.System),
            Content = text
        }, cancellationToken);
    }

    private async Task SyncStateStoreAsync(ConsultationSession session, CancellationToken cancellationToken) =>
        await _stateStore.SetSessionStateAsync(
            session.Id,
            session.Status.ToString(),
            session.LastActivityAt,
            session.PatientUnreadCount,
            session.DoctorUnreadCount,
            cancellationToken);

    private async Task<ConsultationSession> RequireDoctorSessionAsync(
        Guid sessionId,
        Guid doctorId,
        CancellationToken cancellationToken)
    {
        var session = await _sessions.GetByIdForUpdateAsync(sessionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Session {sessionId} not found.");
        if (session.DoctorId != doctorId)
            throw new UnauthorizedAccessException("Only the assigned doctor can perform this action.");
        EnsureNotTerminal(session);
        return session;
    }

    private static void EnsureParticipant(ConsultationSession session, Guid userId, ParticipantRole role)
    {
        if (role == ParticipantRole.Patient && session.PatientId != userId)
            throw new UnauthorizedAccessException("Invalid patient.");
        if (role == ParticipantRole.Doctor && session.DoctorId != userId)
            throw new UnauthorizedAccessException("Invalid doctor.");
    }

    private static void EnsureNotTerminal(ConsultationSession session)
    {
        if (session.Status is ConsultationStatus.Completed or ConsultationStatus.Cancelled or ConsultationStatus.Expired)
            throw new InvalidOperationException($"Session is {session.Status}.");
    }

    private static ConsultationSessionResponse MapSession(ConsultationSession session) => new()
    {
        SessionId = session.Id,
        PatientId = session.PatientId,
        DoctorId = session.DoctorId,
        DoctorName = session.DoctorName,
        Type = session.Type.ToString(),
        Status = session.Status.ToString(),
        UrgencyLevel = session.UrgencyLevel,
        ExpectedDurationMinutes = session.ExpectedDurationMinutes,
        PatientConsentGiven = session.PatientConsentGiven,
        CreatedAt = session.CreatedAt,
        StartedAt = session.StartedAt,
        CompletedAt = session.CompletedAt,
        PatientUnreadCount = session.PatientUnreadCount,
        DoctorUnreadCount = session.DoctorUnreadCount,
        VideoRoomId = session.VideoRoomId
    };

    private static ConsultationMessageDto MapMessage(ConsultationMessage message) => new()
    {
        MessageId = message.Id,
        SequenceNumber = message.SequenceNumber,
        SenderId = message.SenderId,
        SenderRole = message.SenderRole.ToString(),
        MessageType = message.MessageType.ToString(),
        Content = message.Content,
        AttachmentUrl = message.AttachmentUrl,
        IsImportant = message.IsImportant,
        SentAt = message.SentAt,
        ReadAt = message.ReadAt
    };
}
