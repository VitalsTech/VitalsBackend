using ConsultationService.Domain.Enums;

namespace ConsultationService.Domain.Entities;

public sealed class ConsultationSession
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public string? DoctorName { get; set; }
    public ConsultationType Type { get; set; }
    public ConsultationStatus Status { get; set; }
    public int UrgencyLevel { get; set; }
    public int ExpectedDurationMinutes { get; set; }
    public Guid? RoutingDecisionId { get; set; }
    public Guid? TriageSessionId { get; set; }
    public bool PatientConsentGiven { get; set; }
    public DateTime? PatientConsentAt { get; set; }
    public bool VideoRecordingConsent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? PausedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public Guid? LastMessageId { get; set; }
    public long LastSequenceNumber { get; set; }
    public int PatientUnreadCount { get; set; }
    public int DoctorUnreadCount { get; set; }
    public int? PatientRating { get; set; }
    public string? PatientFeedback { get; set; }
    public int? DoctorRating { get; set; }
    public string? DoctorFeedback { get; set; }
    public string? CancelReason { get; set; }
    public string? VideoRoomId { get; set; }
    public string ProtocolJson { get; set; } = "{}";
}
