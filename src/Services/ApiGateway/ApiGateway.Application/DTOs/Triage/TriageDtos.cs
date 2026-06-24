namespace ApiGateway.Application.DTOs.Triage;

public sealed class CreateTriageSessionRequestDto
{
    public Guid PatientId { get; set; }
    public string? ChiefComplaint { get; set; }
    public string? Locale { get; set; }
}

public sealed class SendTriageMessageRequestDto
{
    public string Content { get; set; } = string.Empty;
}
