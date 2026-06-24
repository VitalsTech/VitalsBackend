namespace RoutingService.Domain.Entities;

public sealed class PatientRoute
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid? CurrentDecisionId { get; set; }
    public string Status { get; set; } = "active";
    public int CurrentStep { get; set; } = 1;
    public int TotalSteps { get; set; } = 1;
    public string StepsJson { get; set; } = "[]";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
