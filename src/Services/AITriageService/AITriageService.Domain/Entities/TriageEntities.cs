namespace AITriageService.Domain.Entities;

public class TriageSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PatientId { get; set; }
    public Guid? CorrelationId { get; set; }
    public string Status { get; set; } = "Active";
    public int LatestUrgencyLevel { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TriageMessage> Messages { get; set; } = new List<TriageMessage>();
    public ICollection<TriageAssessment> Assessments { get; set; } = new List<TriageAssessment>();
}

public class TriageMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public string Role { get; set; } = "Patient";
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public TriageSession Session { get; set; } = null!;
}

public class TriageAssessment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public Guid MessageId { get; set; }
    public int UrgencyLevel { get; set; }
    public string ExtractedEntitiesJson { get; set; } = "[]";
    public string NerEntitiesJson { get; set; } = "[]";
    public string LlmResultJson { get; set; } = "{}";
    public string AssistantReply { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public TriageSession Session { get; set; } = null!;
}
