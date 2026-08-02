namespace AITriageService.Application.DTOs;

public sealed class CreateTriageSessionRequest
{
    public Guid PatientId { get; set; }
    public Guid? CorrelationId { get; set; }
}

public sealed class TriageSessionResponse
{
    public Guid SessionId { get; set; }
    public Guid PatientId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int LatestUrgencyLevel { get; set; }
    public DateTime CreatedAt { get; set; }
    public IReadOnlyList<TriageMessageDto> Messages { get; set; } = Array.Empty<TriageMessageDto>();
    public TriageAssessmentDto? LatestAssessment { get; set; }
    /// <summary>Frontend alias: emergency | urgent | routine.</summary>
    public string? Urgency { get; set; }
    /// <summary>Frontend alias for RecommendedAction.</summary>
    public string? Recommendation { get; set; }
    public string? RecommendationText { get; set; }
    public string? RecommendedSpecialization { get; set; }
    public bool CanBeRemote { get; set; } = true;

    /// <summary>
    /// ИИ считает анамнез достаточным — фронт показывает CTA «Завершить триаж»
    /// → POST .../complete.
    /// </summary>
    public bool ReadyToComplete { get; set; }

    /// <summary>Короткий текст предложения завершить (если ReadyToComplete).</summary>
    public string? CompleteSuggestion { get; set; }

    /// <summary>Заполняется после complete, если routing отработал (Kafka off → sync HTTP).</summary>
    public Guid? RoutingDecisionId { get; set; }
    public string? RoutingOutcomeType { get; set; }
    public Guid? AssignedDoctorId { get; set; }
    public string? AssignedDoctorName { get; set; }
    public IReadOnlyList<string> RecommendedLabs { get; set; } = Array.Empty<string>();
    public Guid? ConsultationSessionId { get; set; }
}

public sealed class RoutingDecisionSummaryDto
{
    public Guid DecisionId { get; set; }
    public Guid PatientId { get; set; }
    public string OutcomeType { get; set; } = string.Empty;
    public string? Specialist { get; set; }
    public Guid? AssignedDoctorId { get; set; }
    public string? AssignedDoctorName { get; set; }
    public IReadOnlyList<string> RecommendedLabs { get; set; } = Array.Empty<string>();
    public string PatientMessage { get; set; } = string.Empty;
    /// <summary>Сессия консультации, созданная routing (если врач найден).</summary>
    public Guid? ConsultationSessionId { get; set; }
}

public sealed class SendTriageMessageRequest
{
    public string Message { get; set; } = string.Empty;
}

public sealed class TriageMessageDto
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public sealed class TriageAssessmentDto
{
    public int UrgencyLevel { get; set; }
    public IReadOnlyList<ExtractedEntityDto> ExtractedEntities { get; set; } = Array.Empty<ExtractedEntityDto>();
    public IReadOnlyList<NerEntityDto> NerEntities { get; set; } = Array.Empty<NerEntityDto>();
    public LlmTriageResultDto LlmResult { get; set; } = new();
    public string AssistantReply { get; set; } = string.Empty;
}

public sealed class ExtractedEntityDto
{
    public string Type { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Detail { get; set; }
}

public sealed class NerEntityDto
{
    public string Text { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? NormalizedTerm { get; set; }
    public string? Icd10Code { get; set; }
}

public sealed class LlmTriageResultDto
{
    public IReadOnlyList<HypothesisDto> Hypotheses { get; set; } = Array.Empty<HypothesisDto>();
    public int UrgencyLevel { get; set; }
    public string NextQuestion { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
    public IReadOnlyList<string> AdditionalDataNeeded { get; set; } = Array.Empty<string>();
    public bool EmergencyWarning { get; set; }

    /// <summary>Анамнез достаточный, можно предложить завершить триаж.</summary>
    public bool ReadyToComplete { get; set; }

    /// <summary>Текст для пациента: почему можно завершать / призыв нажать «Завершить».</summary>
    public string? CompleteSuggestion { get; set; }
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

public sealed class TriagePromptContext
{
    public Guid SessionId { get; set; }
    public Guid PatientId { get; set; }
    public string CurrentMessage { get; set; } = string.Empty;
    public IReadOnlyList<TriageMessageDto> DialogHistory { get; set; } = Array.Empty<TriageMessageDto>();
    public IReadOnlyList<ExtractedEntityDto> ParsedEntities { get; set; } = Array.Empty<ExtractedEntityDto>();
    public IReadOnlyList<NerEntityDto> NerEntities { get; set; } = Array.Empty<NerEntityDto>();
    public PatientMedicalContextDto? MedicalContext { get; set; }
}
