namespace RoutingService.Domain.Entities;

public sealed class ClinicRoutingRule
{
    public Guid Id { get; set; }
    public string ClinicId { get; set; } = "default";
    public string RuleKey { get; set; } = string.Empty;
    public string RuleValue { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; }
}
