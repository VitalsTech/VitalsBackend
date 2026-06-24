namespace RoutingService.Domain.Entities;

public sealed class RoutingAuditEntry
{
    public Guid Id { get; set; }
    public Guid DecisionId { get; set; }
    public string InputEventJson { get; set; } = string.Empty;
    public string MedicalContextJson { get; set; } = string.Empty;
    public string RejectedAlternativesJson { get; set; } = "[]";
    public string PublishedEventsJson { get; set; } = "[]";
    public string AlgorithmVersion { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public RoutingDecision? Decision { get; set; }
}
