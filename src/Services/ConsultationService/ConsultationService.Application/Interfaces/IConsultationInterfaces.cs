using ConsultationService.Application.DTOs;
using ConsultationService.Domain.Entities;
using ConsultationService.Domain.Enums;

namespace ConsultationService.Application.Interfaces;

public interface IConsultationRepository
{
    Task<ConsultationSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<ConsultationSession?> GetByIdForUpdateAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task SaveSessionAsync(ConsultationSession session, CancellationToken cancellationToken = default);
    Task AddTransitionAsync(SessionStatusTransition transition, CancellationToken cancellationToken = default);
    Task AddParticipantAsync(SessionParticipant participant, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConsultationSession>> GetExpiredCandidatesAsync(DateTime utcNow, CancellationToken cancellationToken = default);
}

public interface IMessageRepository
{
    Task<ConsultationMessage> AddMessageAsync(ConsultationMessage message, CancellationToken cancellationToken = default);
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
}

public interface IConsultationEventPublisher
{
    Task PublishAsync(string topic, object payload, CancellationToken cancellationToken = default);
}

public interface ISfuSignalingService
{
    Task<VideoRoomResponse> CreateRoomAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task CloseRoomAsync(string roomId, CancellationToken cancellationToken = default);
}

public interface IConsultationChatNotifier
{
    Task NotifyMessageAsync(Guid sessionId, ConsultationMessageDto message, CancellationToken cancellationToken = default);
    Task NotifyStatusChangedAsync(Guid sessionId, string status, CancellationToken cancellationToken = default);
}

public interface IConsultationService
{
    Task<ConsultationSessionResponse> CreateFromRoutingDecisionAsync(RoutingDecisionEventDto decision, CancellationToken cancellationToken = default);
    Task<ConsultationSessionResponse> CreateManualAsync(CreateConsultationRequest request, CancellationToken cancellationToken = default);
    Task<ConsultationSessionResponse> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<ConsultationSessionResponse> JoinAsync(Guid sessionId, Guid userId, ParticipantRole role, CancellationToken cancellationToken = default);
    Task<ConsultationSessionResponse> PauseAsync(Guid sessionId, Guid doctorId, CancellationToken cancellationToken = default);
    Task<ConsultationSessionResponse> ResumeAsync(Guid sessionId, Guid doctorId, CancellationToken cancellationToken = default);
    Task<ConsultationSessionResponse> DoctorLeaveAsync(Guid sessionId, Guid doctorId, CancellationToken cancellationToken = default);
    Task<ConsultationSessionResponse> CompleteAsync(Guid sessionId, Guid doctorId, CompleteConsultationRequest request, CancellationToken cancellationToken = default);
    Task<ConsultationSessionResponse> PatientConfirmAsync(Guid sessionId, Guid patientId, CancellationToken cancellationToken = default);
    Task<ConsultationSessionResponse> CancelAsync(Guid sessionId, Guid userId, string reason, CancellationToken cancellationToken = default);
    Task<ConsultationSessionResponse> RecordConsentAsync(Guid sessionId, Guid patientId, ConsentRequest request, CancellationToken cancellationToken = default);
    Task<ConsultationMessageDto> SendMessageAsync(Guid sessionId, Guid senderId, ParticipantRole role, SendMessageRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConsultationMessageDto>> GetMessagesAsync(Guid sessionId, long afterSequence, CancellationToken cancellationToken = default);
    Task<VideoRoomResponse> StartVideoAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken = default);
    Task TriggerEmergencyAsync(Guid sessionId, Guid doctorId, CancellationToken cancellationToken = default);
    Task SubmitRatingAsync(Guid sessionId, Guid userId, SubmitRatingRequest request, CancellationToken cancellationToken = default);
    Task<ConsultationSessionResponse> InviteDoctorAsync(Guid sessionId, Guid initiatorDoctorId, InviteDoctorRequest request, CancellationToken cancellationToken = default);
    Task ExpireAsync(Guid sessionId, string reason, CancellationToken cancellationToken = default);
}
