namespace ApiGateway.Application.DTOs.Triage;

public sealed class CreateTriageSessionRequestDto
{
    public Guid PatientId { get; set; }
    public string? ChiefComplaint { get; set; }
    public string? Locale { get; set; }
}

public sealed class SendTriageMessageRequestDto
{
    /// <summary>Основной контракт фронта / AITriage.</summary>
    public string? Message { get; set; }

    /// <summary>Алиас (старый gateway-контракт).</summary>
    public string? Content { get; set; }

    public string Text =>
        !string.IsNullOrWhiteSpace(Message) ? Message.Trim()
        : !string.IsNullOrWhiteSpace(Content) ? Content.Trim()
        : string.Empty;
}
