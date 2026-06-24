using ApiGateway.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.API.Controllers.V1;

[ApiController]
[Route("api/v1/analytics")]
[Authorize]
public sealed class AnalyticsController : GatewayControllerBase
{
    private readonly IBackendForwarder _backend;

    public AnalyticsController(IBackendForwarder backend) => _backend = backend;

    [HttpGet("dashboard")]
    public Task<IActionResult> Dashboard(CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("analytics", HttpMethod.Get, "api/analytics/dashboard", ForwardContext, cancellationToken: cancellationToken), cancellationToken);
}
