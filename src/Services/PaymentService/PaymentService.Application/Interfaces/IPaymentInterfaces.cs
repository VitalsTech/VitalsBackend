using PaymentService.Application.DTOs;
using PaymentService.Domain.Entities;

namespace PaymentService.Application.Interfaces;

public interface IPaymentRepository
{
    Task<PaymentTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task SaveAsync(PaymentTransaction payment, CancellationToken cancellationToken = default);
}

public interface IPaymentGatewayClient
{
    Task<PaymentGatewayResult> ProcessPaymentAsync(PaymentTransaction payment, CancellationToken cancellationToken = default);
}

public sealed class PaymentGatewayResult
{
    public bool Success { get; init; }
    public string? ExternalReference { get; init; }
    public string? FailureReason { get; init; }
}

public interface IPaymentEventPublisher
{
    Task PublishAsync(string topic, object payload, CancellationToken cancellationToken = default);
}

public interface IPaymentService
{
    Task<PaymentResponse> CreatePaymentAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default);
    Task<PaymentResponse> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default);
    Task<PaymentResponse> HandleWebhookAsync(PaymentWebhookRequest request, CancellationToken cancellationToken = default);
}
