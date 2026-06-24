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
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int PatientUnreadCount { get; set; }
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
    public string RoomId { get; set; } = string.Empty;
    public string ServerUrl { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
}

public sealed class InviteDoctorRequest
{
    public Guid DoctorId { get; set; }
    public string? DoctorName { get; set; }
}
