namespace PaymentService.Application.DTOs;

public sealed class CreatePaymentRequest
{
    public Guid UserId { get; set; }
    public Guid? ConsultationId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "RUB";
    public string? PaymentMethodToken { get; set; }
}

public sealed class PaymentResponse
{
    public Guid PaymentId { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "RUB";
    public string Status { get; set; } = string.Empty;
    public string? ExternalReference { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public sealed class PaymentWebhookRequest
{
    public Guid PaymentId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ExternalReference { get; set; }
    public string? FailureReason { get; set; }
}
