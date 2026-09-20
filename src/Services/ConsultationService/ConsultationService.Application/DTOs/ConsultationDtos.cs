using System.Text.Json;

namespace ConsultationService.Application.DTOs;

public sealed class RoutingDecisionEventDto
{
    public Guid PatientId { get; set; }
    public Guid SessionId { get; set; }
    public Guid? RoutingDecisionId { get; set; }
    public string OutcomeType { get; set; } = string.Empty;
    public string? Specialist { get; set; }
    public string? ConsultationFormat { get; set; }
    public Guid? DoctorId { get; set; }
    public string? DoctorName { get; set; }
    public int Priority { get; set; }
    public int EffectiveUrgencyLevel { get; set; }
    public string? PatientMessage { get; set; }
}

public sealed class CreateConsultationRequest
{
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public string? DoctorName { get; set; }
    public string ConsultationType { get; set; } = "SyncChat";
    public int UrgencyLevel { get; set; } = 3;
    public Guid? RoutingDecisionId { get; set; }
    public Guid? TriageSessionId { get; set; }

    /// <summary>Время приёма из брони слота. Задано — сессия всегда создаётся новая.</summary>
    public DateTime? ScheduledAt { get; set; }

    public Guid? ScheduledSlotId { get; set; }
}

public sealed class ConsultationSessionResponse
{
    public Guid SessionId { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public string? DoctorName { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int UrgencyLevel { get; set; }
    public int ExpectedDurationMinutes { get; set; }
    public bool PatientConsentGiven { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public Guid? ScheduledSlotId { get; set; }

    /// <summary>true — запись на слот расписания; false — свободный чат / консультация без брони.</summary>
    public bool IsScheduled { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public int PatientUnreadCount { get; set; }
    public int DoctorUnreadCount { get; set; }
    public string? VideoRoomId { get; set; }
    public bool VideoActive { get; set; }

    /// <summary>Протокол приёма — заполняется после POST .../complete.</summary>
    public CompleteConsultationRequest? Protocol { get; set; }

    public string? ProtocolSignature { get; set; }

    /// <summary>true — врач отправил протокол, консультация закрыта.</summary>
    public bool HasProtocol { get; set; }
}

public sealed class MyConsultationsResponse
{
    public IReadOnlyList<ConsultationSessionResponse> Items { get; set; } = Array.Empty<ConsultationSessionResponse>();
}

/// <summary>
/// Compact session projection consumed by the gateway when building the doctor calendar.
/// </summary>
public sealed class DoctorCalendarSessionDto
{
    public Guid SessionId { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsOpen { get; set; }
    public int UrgencyLevel { get; set; }
    public int ExpectedDurationMinutes { get; set; }
    public Guid? TriageSessionId { get; set; }
    public Guid? RoutingDecisionId { get; set; }
    public Guid? ScheduledSlotId { get; set; }

    /// <summary>Якорь для календаря: время брони, иначе фактический старт, иначе создание.</summary>
    public DateTime ScheduledAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public int DoctorUnreadCount { get; set; }
    public string? VideoRoomId { get; set; }
}

public sealed class JoinSessionRequest
{
    public string Role { get; set; } = string.Empty;
}

public sealed class SendMessageRequest
{
    public string MessageType { get; set; } = "Text";
    public string Content { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
    public bool IsImportant { get; set; }
}

public sealed class ConsultationMessageDto
{
    public Guid MessageId { get; set; }
    /// <summary>Alias for frontend clients that expect <c>id</c>.</summary>
    public Guid Id => MessageId;
    public long SequenceNumber { get; set; }
    public Guid SenderId { get; set; }
    public string SenderRole { get; set; } = string.Empty;
    public string MessageType { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
    public bool IsImportant { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime? ReadAt { get; set; }
}

public sealed class CompleteConsultationRequest
{
    public string Complaints { get; set; } = string.Empty;
    public string Anamnesis { get; set; } = string.Empty;
    public string? ExaminationNotes { get; set; }
    public string PreliminaryDiagnosisIcd10 { get; set; } = string.Empty;
    public string PreliminaryDiagnosisText { get; set; } = string.Empty;
    public string Recommendations { get; set; } = string.Empty;
    public IReadOnlyList<string> Prescriptions { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> LabOrders { get; set; } = Array.Empty<string>();
    public DateTime? NextVisitDate { get; set; }
}

public sealed class CancelSessionRequest
{
    public string Reason { get; set; } = string.Empty;
}

public sealed class ConsentRequest
{
    public bool DataProcessingConsent { get; set; }
    public bool VideoRecordingConsent { get; set; }
}

public sealed class SubmitRatingRequest
{
    public string Role { get; set; } = string.Empty;
    public int Score { get; set; }
    public string? Feedback { get; set; }
    public int? ClarityScore { get; set; }
    public int? TimelinessScore { get; set; }
    public string? ProblemResolved { get; set; }
}

public sealed class VideoRoomResponse
{
    /// <summary>p2p — WebRTC через SignalR (DEV). sfu — LiveKit/внешний SFU.</summary>
    public string Mode { get; set; } = "p2p";
    public string RoomId { get; set; } = string.Empty;
    public string ServerUrl { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool ChatAvailable { get; set; } = true;
    public string SignalingHub { get; set; } = "/api/v1/consultations/hub";
    public IReadOnlyList<IceServerDto> IceServers { get; set; } = Array.Empty<IceServerDto>();
}

public sealed class IceServerDto
{
    public string Urls { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? Credential { get; set; }
}

public sealed class RtcSignalDto
{
    /// <summary>offer | answer | ice | hangup | media</summary>
    public string Type { get; set; } = string.Empty;
    public string? Sdp { get; set; }
    public string? Candidate { get; set; }
    public string? SdpMid { get; set; }
    public int? SdpMLineIndex { get; set; }
    public bool? Audio { get; set; }
    public bool? Video { get; set; }
}

public sealed class AddDiagnosisRequest
{
    public string Icd10 { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public sealed class AddPrescriptionsRequest
{
    public IReadOnlyList<string> Lines { get; set; } = Array.Empty<string>();
}

public sealed class IssueCertificateRequest
{
    /// <summary>HealthStatus | StudyExcuse | WorkExcuse | Other</summary>
    public string Type { get; set; } = "HealthStatus";
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
}

public sealed class ClinicalActionDto
{
    public Guid Id { get; set; }
    public string Kind { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Guid CreatedByDoctorId { get; set; }
    public JsonElement Payload { get; set; }
}

public sealed class ClinicalActionsResponse
{
    public IReadOnlyList<ClinicalActionDto> Items { get; set; } = Array.Empty<ClinicalActionDto>();
}

public sealed class InviteDoctorRequest
{
    public Guid DoctorId { get; set; }
    public string? DoctorName { get; set; }
}
