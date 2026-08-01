using ApiGateway.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.API.Controllers.V1;

[ApiController]
[Route("api/v1/routing")]
[Authorize]
public sealed class RoutingController : GatewayControllerBase
{
    private readonly IBackendForwarder _backend;

    public RoutingController(IBackendForwarder backend) => _backend = backend;

    [HttpGet("decisions/{decisionId:guid}")]
    public Task<IActionResult> GetDecision(Guid decisionId, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("routing", HttpMethod.Get, $"api/routing/decisions/{decisionId}", ForwardContext, cancellationToken: cancellationToken), cancellationToken);

    [HttpGet("patients/{patientId:guid}/active-route")]
    public Task<IActionResult> GetActiveRoute(Guid patientId, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("routing", HttpMethod.Get, $"api/routing/patients/{patientId}/active-route", ForwardContext, cancellationToken: cancellationToken), cancellationToken);
}
