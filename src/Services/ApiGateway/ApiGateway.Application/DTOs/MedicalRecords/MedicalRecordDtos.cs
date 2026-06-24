namespace ApiGateway.Application.DTOs.MedicalRecords;

public sealed class AppendEventRequestDto
{
    public string EventType { get; set; } = string.Empty;
    public string SourceService { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime? OccurredAt { get; set; }
}

public sealed class CreateAccessGrantRequestDto
{
    public Guid GranteeUserId { get; set; }
    public string GranteeRole { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public string? Reason { get; set; }
}
