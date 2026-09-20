using ConsultationService.Application.DTOs;
using ConsultationService.Domain.Entities;
using ConsultationService.Domain.Enums;

namespace ConsultationService.Application.Interfaces;

public interface IConsultationRepository
{
    Task<ConsultationSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<ConsultationSession?> GetByIdForUpdateAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<ConsultationSession?> FindActiveBetweenAsync(
        Guid patientId,
        Guid doctorId,
        IReadOnlyList<Guid>? alternateDoctorIds = null,
        IReadOnlyList<Guid>? alternatePatientIds = null,
        CancellationToken cancellationToken = default);
    Task<ConsultationSession?> FindLatestByPatientAsync(Guid patientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// True if any doctor identity has any session (incl. completed) with any of the patient identities.
    /// </summary>
    Task<bool> ExistsForDoctorsAndPatientsAsync(
        IReadOnlyList<Guid> doctorIds,
        IReadOnlyList<Guid> patientIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Doctor calendar source: sessions anchored (ScheduledAt ?? StartedAt ?? CreatedAt) inside the window,
    /// plus open sessions with activity since <paramref name="openSince"/> regardless of date.
    /// </summary>
    Task<IReadOnlyList<ConsultationSession>> GetByDoctorInRangeAsync(
        IReadOnlyList<Guid> doctorIds,
        DateTime from,
        DateTime to,
        DateTime? openSince = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sessions where the user is patient or doctor (by any of their identity ids).
    /// </summary>
    Task<IReadOnlyList<ConsultationSession>> ListForParticipantAsync(
        IReadOnlyList<Guid> identityIds,
        bool asPatient,
        bool asDoctor,
        bool includeCompleted,
        int limit,
        CancellationToken cancellationToken = default);

    Task SaveSessionAsync(ConsultationSession session, CancellationToken cancellationToken = default);
    Task AddTransitionAsync(SessionStatusTransition transition, CancellationToken cancellationToken = default);
    Task<bool> AddParticipantAsync(SessionParticipant participant, CancellationToken cancellationToken = default);
    Task<bool> IsParticipantAsync(Guid sessionId, IReadOnlyList<Guid> identityIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConsultationSession>> GetExpiredCandidatesAsync(DateTime utcNow, CancellationToken cancellationToken = default);
}

public interface IMessageRepository
{
    /// <summary>
    /// Inserts message with the next sequence under a session row lock, and updates session counters.
    /// </summary>
    Task<ConsultationMessage> AddMessageAsync(
        ConsultationSession session,
        ConsultationMessage message,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConsultationMessage>> GetMessagesAfterSequenceAsync(Guid sessionId, long afterSequence, CancellationToken cancellationToken = default);
    Task MarkReadAsync(Guid sessionId, ParticipantRole readerRole, CancellationToken cancellationToken = default);
}

public interface ISessionStateStore
{
    Task SetSessionStateAsync(Guid sessionId, string status, DateTime lastActivity, int patientUnread, int doctorUnread, CancellationToken cancellationToken = default);
}

public interface IMedicalRecordEventClient
{
    Task AppendConsultationEventAsync(Guid patientId, string eventType, object payload, Guid correlationId, CancellationToken cancellationToken = default);
    Task GrantDoctorAccessAsync(Guid patientId, Guid doctorId, CancellationToken cancellationToken = default);
}

public interface ILabOrderClient
{
    Task CreateFromConsultationAsync(
        Guid patientId,
        Guid doctorId,
        Guid consultationId,
        IReadOnlyList<string> labNames,
        CancellationToken cancellationToken = default);
}

public interface IPrescriptionClient
{
    Task CreateFromConsultationAsync(
        Guid patientId,
        Guid doctorId,
        Guid consultationId,
        string? diagnosis,
        IReadOnlyList<string> prescriptionLines,
        CancellationToken cancellationToken = default);
}

public interface IRoutingClient
{
    Task AppendPostConsultationLabsAsync(
        Guid patientId,
        Guid doctorId,
        Guid consultationId,
        IReadOnlyList<string> labs,
        CancellationToken cancellationToken = default);
}

public interface IConsultationEventPublisher
{
    Task PublishAsync(string topic, object payload, CancellationToken cancellationToken = default);
}

public interface ISfuSignalingService
{
    Task<VideoRoomResponse> CreateRoomAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<VideoRoomResponse> IssueCredentialsAsync(Guid sessionId, Guid participantId, string role, CancellationToken cancellationToken = default);
    Task CloseRoomAsync(string roomId, CancellationToken cancellationToken = default);
}

public interface IConsultationChatNotifier
{
    Task NotifyMessageAsync(Guid sessionId, ConsultationMessageDto message, CancellationToken cancellationToken = default);
    Task NotifyStatusChangedAsync(Guid sessionId, string status, CancellationToken cancellationToken = default);
    Task NotifyMessagesReadAsync(Guid sessionId, ParticipantRole readerRole, DateTime readAt, long lastSequence, CancellationToken cancellationToken = default);
    Task NotifyVideoStartedAsync(Guid sessionId, VideoRoomResponse room, CancellationToken cancellationToken = default);
    Task NotifyVideoStoppedAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task NotifyClinicalActionAsync(Guid sessionId, ClinicalActionDto action, CancellationToken cancellationToken = default);
}

public interface IClinicalActionRepository
{
    Task AddAsync(ConsultationClinicalAction action, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConsultationClinicalAction>> ListBySessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
}

public interface IConsultationService
{
    Task<ConsultationSessionResponse> CreateFromRoutingDecisionAsync(RoutingDecisionEventDto decision, CancellationToken cancellationToken = default);
    Task<ConsultationSessionResponse> CreateManualAsync(CreateConsultationRequest request, CancellationToken cancellationToken = default);
    Task<ConsultationSessionResponse> OpenOrCreateAsync(CreateConsultationRequest request, CancellationToken cancellationToken = default);
    Task<ConsultationSessionResponse> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<ConsultationSessionResponse?> FindActiveAsync(Guid patientId, Guid doctorId, CancellationToken cancellationToken = default);

    /// <summary>Список консультаций текущего пользователя (пациент и/или врач).</summary>
    Task<IReadOnlyList<ConsultationSessionResponse>> ListMineAsync(
        IReadOnlyList<Guid> identityIds,
        bool asPatient,
        bool asDoctor,
        bool includeCompleted,
        int limit,
        CancellationToken cancellationToken = default);
    Task<ConsultationSessionResponse> JoinAsync(Guid sessionId, Guid userId, ParticipantRole role, IReadOnlyList<Guid>? identityIds = null, CancellationToken cancellationToken = default);
    Task<ConsultationSessionResponse> PauseAsync(Guid sessionId, Guid doctorId, IReadOnlyList<Guid>? identityIds = null, CancellationToken cancellationToken = default);
    Task<ConsultationSessionResponse> ResumeAsync(Guid sessionId, Guid doctorId, IReadOnlyList<Guid>? identityIds = null, CancellationToken cancellationToken = default);
    Task<ConsultationSessionResponse> DoctorLeaveAsync(Guid sessionId, Guid doctorId, IReadOnlyList<Guid>? identityIds = null, CancellationToken cancellationToken = default);
    Task<ConsultationSessionResponse> CompleteAsync(Guid sessionId, Guid doctorId, CompleteConsultationRequest request, IReadOnlyList<Guid>? identityIds = null, CancellationToken cancellationToken = default);
    Task<ConsultationSessionResponse> PatientConfirmAsync(Guid sessionId, Guid patientId, IReadOnlyList<Guid>? identityIds = null, CancellationToken cancellationToken = default);
    Task<ConsultationSessionResponse> CancelAsync(Guid sessionId, Guid userId, string reason, IReadOnlyList<Guid>? identityIds = null, CancellationToken cancellationToken = default);
    Task<ConsultationSessionResponse> RecordConsentAsync(Guid sessionId, Guid patientId, ConsentRequest request, IReadOnlyList<Guid>? identityIds = null, CancellationToken cancellationToken = default);
    Task<ConsultationMessageDto> SendMessageAsync(Guid sessionId, Guid senderId, ParticipantRole role, SendMessageRequest request, IReadOnlyList<Guid>? identityIds = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConsultationMessageDto>> GetMessagesAsync(
        Guid sessionId,
        long afterSequence,
        Guid readerId,
        ParticipantRole readerRole,
        bool markAsRead,
        IReadOnlyList<Guid>? identityIds = null,
        CancellationToken cancellationToken = default);
    Task<ConsultationSessionResponse> MarkMessagesReadAsync(
        Guid sessionId,
        Guid readerId,
        ParticipantRole readerRole,
        IReadOnlyList<Guid>? identityIds = null,
        CancellationToken cancellationToken = default);
    Task<VideoRoomResponse> StartVideoAsync(Guid sessionId, Guid userId, IReadOnlyList<Guid>? identityIds = null, CancellationToken cancellationToken = default);
    Task<VideoRoomResponse> JoinVideoAsync(Guid sessionId, Guid userId, IReadOnlyList<Guid>? identityIds = null, CancellationToken cancellationToken = default);
    Task StopVideoAsync(Guid sessionId, Guid userId, IReadOnlyList<Guid>? identityIds = null, CancellationToken cancellationToken = default);
    Task<ClinicalActionDto> AddDiagnosisAsync(Guid sessionId, Guid doctorId, AddDiagnosisRequest request, IReadOnlyList<Guid>? identityIds = null, CancellationToken cancellationToken = default);
    Task<ClinicalActionDto> AddPrescriptionsAsync(Guid sessionId, Guid doctorId, AddPrescriptionsRequest request, IReadOnlyList<Guid>? identityIds = null, CancellationToken cancellationToken = default);
    Task<ClinicalActionDto> IssueCertificateAsync(Guid sessionId, Guid doctorId, IssueCertificateRequest request, IReadOnlyList<Guid>? identityIds = null, CancellationToken cancellationToken = default);
    Task<ClinicalActionsResponse> ListClinicalActionsAsync(Guid sessionId, Guid userId, IReadOnlyList<Guid>? identityIds = null, CancellationToken cancellationToken = default);
    Task TriggerEmergencyAsync(Guid sessionId, Guid doctorId, CancellationToken cancellationToken = default);
    Task SubmitRatingAsync(Guid sessionId, Guid userId, SubmitRatingRequest request, CancellationToken cancellationToken = default);
    Task<ConsultationSessionResponse> InviteDoctorAsync(Guid sessionId, Guid initiatorDoctorId, InviteDoctorRequest request, CancellationToken cancellationToken = default);
    Task ExpireAsync(Guid sessionId, string reason, CancellationToken cancellationToken = default);
}
