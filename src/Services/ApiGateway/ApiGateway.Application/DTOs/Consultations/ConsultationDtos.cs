namespace ApiGateway.Application.DTOs.Consultations;

public sealed class CreateConsultationRequestDto
{
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public string? DoctorName { get; set; }
    public string ConsultationType { get; set; } = "SyncChat";
    public string? PrimarySymptom { get; set; }
    public string Urgency { get; set; } = "Normal";
    public int UrgencyLevel { get; set; } = 3;
    public Guid? RoutingDecisionId { get; set; }
    public Guid? TriageSessionId { get; set; }
}

public sealed class JoinSessionRequestDto
{
    public string Role { get; set; } = string.Empty;
}

public sealed class SendMessageRequestDto
{
    public string MessageType { get; set; } = "Text";
    public string Content { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
    public bool IsImportant { get; set; }
}

public sealed class CompleteConsultationRequestDto
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

public sealed class CancelSessionRequestDto
{
    public string Reason { get; set; } = string.Empty;
}

public sealed class ConsentRequestDto
{
    public bool DataProcessingConsent { get; set; }
    public bool VideoRecordingConsent { get; set; }
}

public sealed class SubmitRatingRequestDto
{
    public string Role { get; set; } = string.Empty;
    public int Score { get; set; }
    public string? Feedback { get; set; }
    public int? ClarityScore { get; set; }
    public int? TimelinessScore { get; set; }
    public string? ProblemResolved { get; set; }
}

public sealed class InviteDoctorRequestDto
{
    public Guid DoctorId { get; set; }
    public string? DoctorName { get; set; }
}
