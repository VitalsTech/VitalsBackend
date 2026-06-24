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
