using ApiGateway.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.API.Controllers.V1;

[ApiController]
[Route("api/v1/payments")]
[Authorize]
public sealed class PaymentsController : GatewayControllerBase
{
    private readonly IBackendForwarder _backend;

    public PaymentsController(IBackendForwarder backend) => _backend = backend;

    [HttpPost]
    public Task<IActionResult> Create([FromBody] object request, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardJsonAsync("payment", HttpMethod.Post, "api/payments", ForwardContext, request, cancellationToken), cancellationToken);

    [HttpGet("{paymentId:guid}")]
    public Task<IActionResult> Get(Guid paymentId, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("payment", HttpMethod.Get, $"api/payments/{paymentId}", ForwardContext, cancellationToken: cancellationToken), cancellationToken);
}
