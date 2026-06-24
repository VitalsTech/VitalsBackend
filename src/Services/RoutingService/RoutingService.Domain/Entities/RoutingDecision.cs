using RoutingService.Domain.Enums;

namespace RoutingService.Domain.Entities;

public sealed class RoutingDecision
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid TriageSessionId { get; set; }
    public RoutingOutcomeType OutcomeType { get; set; }
    public string? Specialist { get; set; }
    public ConsultationFormat? ConsultationFormat { get; set; }
    public Guid? AssignedDoctorId { get; set; }
    public string? AssignedDoctorName { get; set; }
    public int Priority { get; set; }
    public int UrgencyLevel { get; set; }
    public string RecommendedLabsJson { get; set; } = "[]";
    public string PatientMessage { get; set; } = string.Empty;
    public string Rationale { get; set; } = string.Empty;
    public string AlgorithmVersion { get; set; } = string.Empty;
    public bool IsFallback { get; set; }
    public DateTime CreatedAt { get; set; }
}
