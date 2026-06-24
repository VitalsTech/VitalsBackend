namespace RoutingService.Application.DTOs;

public sealed class TriageCompletedEventDto
{
    public Guid SessionId { get; set; }
    public Guid PatientId { get; set; }
    public int UrgencyLevel { get; set; }
    public bool EmergencyWarning { get; set; }
    public string? PatientMessageSummary { get; set; }
    public int? PatientAgeYears { get; set; }
    public IReadOnlyList<HypothesisDto> Hypotheses { get; set; } = Array.Empty<HypothesisDto>();
    public IReadOnlyList<string> ExtractedSymptoms { get; set; } = Array.Empty<string>();
    public string? RecommendedAction { get; set; }
}

public sealed class HypothesisDto
{
    public string Condition { get; set; } = string.Empty;
    public double Probability { get; set; }
}

public sealed class PatientMedicalContextDto
{
    public IReadOnlyList<string> ActiveDiagnoses { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> ActiveMedications { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Allergies { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> RecentLabHighlights { get; set; } = Array.Empty<string>();
}

public sealed class RoutingDecisionResponse
{
    public Guid DecisionId { get; set; }
    public Guid PatientId { get; set; }
    public Guid TriageSessionId { get; set; }
    public string OutcomeType { get; set; } = string.Empty;
    public string? Specialist { get; set; }
    public string? ConsultationFormat { get; set; }
    public Guid? AssignedDoctorId { get; set; }
    public string? AssignedDoctorName { get; set; }
    public int Priority { get; set; }
    public int UrgencyLevel { get; set; }
    public IReadOnlyList<string> RecommendedLabs { get; set; } = Array.Empty<string>();
    public string PatientMessage { get; set; } = string.Empty;
    public string Rationale { get; set; } = string.Empty;
    public string AlgorithmVersion { get; set; } = string.Empty;
    public bool IsFallback { get; set; }
    public IReadOnlyList<string> PublishedEvents { get; set; } = Array.Empty<string>();
}

public sealed class PatientActiveRouteResponse
{
    public Guid RouteId { get; set; }
    public Guid PatientId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int CurrentStep { get; set; }
    public int TotalSteps { get; set; }
    public IReadOnlyList<RouteStepDto> Steps { get; set; } = Array.Empty<RouteStepDto>();
}

public sealed class RouteStepDto
{
    public int StepNumber { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public sealed class RoutingEngineInput
{
    public TriageCompletedEventDto TriageEvent { get; set; } = new();
    public PatientMedicalContextDto? MedicalContext { get; set; }
    public IReadOnlyDictionary<string, string> ClinicRules { get; set; } = new Dictionary<string, string>();
}

public sealed class RoutingEngineResult
{
    public string OutcomeType { get; set; } = string.Empty;
    public string? Specialist { get; set; }
    public string? ConsultationFormat { get; set; }
    public int Priority { get; set; }
    public int EffectiveUrgencyLevel { get; set; }
    public IReadOnlyList<string> RecommendedLabs { get; set; } = Array.Empty<string>();
    public string PatientMessage { get; set; } = string.Empty;
    public string Rationale { get; set; } = string.Empty;
    public bool IsFallback { get; set; }
    public IReadOnlyList<RejectedAlternativeDto> RejectedAlternatives { get; set; } = Array.Empty<RejectedAlternativeDto>();
    public IReadOnlyList<string> EventsToPublish { get; set; } = Array.Empty<string>();
    public IReadOnlyList<RouteStepDto>? PlannedSteps { get; set; }
}

public sealed class RejectedAlternativeDto
{
    public string Option { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public sealed class DoctorSlotDto
{
    public Guid DoctorId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public int TodayLoad { get; set; }
    public bool IsSenior { get; set; }
}
