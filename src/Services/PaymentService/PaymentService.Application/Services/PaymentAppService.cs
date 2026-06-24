using PaymentService.Application.DTOs;
using PaymentService.Application.Interfaces;
using PaymentService.Application.Options;
using PaymentService.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PaymentService.Application.Services;

public sealed class PaymentAppService : IPaymentService
{
    private readonly IPaymentRepository _repo;
    private readonly IPaymentGatewayClient _gateway;
    private readonly IPaymentEventPublisher _publisher;
    private readonly KafkaOptions _kafka;
    private readonly ILogger<PaymentAppService> _logger;

    public PaymentAppService(
        IPaymentRepository repo,
        IPaymentGatewayClient gateway,
        IPaymentEventPublisher publisher,
        IOptions<KafkaOptions> kafka,
        ILogger<PaymentAppService> logger)
    {
        _repo = repo;
        _gateway = gateway;
        _publisher = publisher;
        _kafka = kafka.Value;
        _logger = logger;
    }

    public async Task<PaymentResponse> CreatePaymentAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        var payment = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            ConsultationId = request.ConsultationId,
            Amount = request.Amount,
            Currency = request.Currency,
            PaymentMethodToken = request.PaymentMethodToken,
            Status = PaymentStatuses.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.SaveAsync(payment, cancellationToken).ConfigureAwait(false);

        var result = await _gateway.ProcessPaymentAsync(payment, cancellationToken).ConfigureAwait(false);
        if (result.Success)
        {
            payment.Status = PaymentStatuses.Completed;
            payment.ExternalReference = result.ExternalReference;
            payment.CompletedAt = DateTime.UtcNow;
            await _repo.SaveAsync(payment, cancellationToken).ConfigureAwait(false);
            await PublishStatusAsync(payment, _kafka.PaymentCompletedTopic, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            payment.Status = PaymentStatuses.Failed;
            payment.FailureReason = result.FailureReason ?? "Payment declined";
            await _repo.SaveAsync(payment, cancellationToken).ConfigureAwait(false);
            await PublishStatusAsync(payment, _kafka.PaymentFailedTopic, cancellationToken).ConfigureAwait(false);
        }

        return Map(payment);
    }

    public async Task<PaymentResponse> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        var payment = await _repo.GetByIdAsync(paymentId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Payment {paymentId} not found.");
        return Map(payment);
    }

    public async Task<PaymentResponse> HandleWebhookAsync(PaymentWebhookRequest request, CancellationToken cancellationToken = default)
    {
        var payment = await _repo.GetByIdAsync(request.PaymentId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Payment {request.PaymentId} not found.");

        payment.Status = request.Status;
        payment.ExternalReference = request.ExternalReference ?? payment.ExternalReference;
        payment.FailureReason = request.FailureReason;
        payment.CompletedAt = request.Status == PaymentStatuses.Completed ? DateTime.UtcNow : payment.CompletedAt;
        await _repo.SaveAsync(payment, cancellationToken).ConfigureAwait(false);

        var topic = request.Status == PaymentStatuses.Failed ? _kafka.PaymentFailedTopic : _kafka.PaymentCompletedTopic;
        await PublishStatusAsync(payment, topic, cancellationToken).ConfigureAwait(false);
        return Map(payment);
    }

    private async Task PublishStatusAsync(PaymentTransaction payment, string topic, CancellationToken cancellationToken)
    {
        try
        {
            await _publisher.PublishAsync(topic, new
            {
                payment.Id,
                payment.UserId,
                payment.ConsultationId,
                payment.Amount,
                payment.Currency,
                payment.Status,
                payment.FailureReason
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish payment event to {Topic}", topic);
        }
    }

    private static PaymentResponse Map(PaymentTransaction payment) => new()
    {
        PaymentId = payment.Id,
        UserId = payment.UserId,
        Amount = payment.Amount,
        Currency = payment.Currency,
        Status = payment.Status,
        ExternalReference = payment.ExternalReference,
        FailureReason = payment.FailureReason,
        CreatedAt = payment.CreatedAt,
        CompletedAt = payment.CompletedAt
    };
}
