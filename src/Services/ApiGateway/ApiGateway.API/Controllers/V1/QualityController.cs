using ApiGateway.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.API.Controllers.V1;

[ApiController]
[Route("api/v1/quality")]
[Authorize]
public sealed class QualityController : GatewayControllerBase
{
    private readonly IBackendForwarder _backend;

    public QualityController(IBackendForwarder backend) => _backend = backend;

    [HttpGet("metrics")]
    public Task<IActionResult> Metrics(CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("quality", HttpMethod.Get, "api/quality/metrics", ForwardContext, cancellationToken: cancellationToken), cancellationToken);
}
