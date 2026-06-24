using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Application.DTOs;
using PaymentService.Application.Interfaces;

namespace PaymentService.API.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public sealed class PaymentsController : ControllerBase
{
    private readonly IPaymentService _payments;

    public PaymentsController(IPaymentService payments) => _payments = payments;

    [HttpPost]
    public Task<PaymentResponse> Create([FromBody] CreatePaymentRequest request, CancellationToken cancellationToken) =>
        _payments.CreatePaymentAsync(request, cancellationToken);

    [HttpGet("{paymentId:guid}")]
    public Task<PaymentResponse> Get(Guid paymentId, CancellationToken cancellationToken) =>
        _payments.GetPaymentAsync(paymentId, cancellationToken);
}

[ApiController]
[Route("internal/payments")]
[Authorize(Policy = "InternalService")]
public sealed class InternalPaymentsController : ControllerBase
{
    private readonly IPaymentService _payments;

    public InternalPaymentsController(IPaymentService payments) => _payments = payments;

    [HttpPost("webhook")]
    public Task<PaymentResponse> Webhook([FromBody] PaymentWebhookRequest request, CancellationToken cancellationToken) =>
        _payments.HandleWebhookAsync(request, cancellationToken);

    [HttpGet("{paymentId:guid}")]
    public Task<PaymentResponse> Get(Guid paymentId, CancellationToken cancellationToken) =>
        _payments.GetPaymentAsync(paymentId, cancellationToken);
}
