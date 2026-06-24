using ApiGateway.Application.DTOs.Triage;
using ApiGateway.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.API.Controllers.V1;

[ApiController]
[Route("api/v1/triage/sessions")]
[Authorize]
public sealed class TriageController : GatewayControllerBase
{
    private readonly IBackendForwarder _backend;

    public TriageController(IBackendForwarder backend) => _backend = backend;

    [HttpPost]
    public Task<IActionResult> CreateSession([FromBody] CreateTriageSessionRequestDto request, CancellationToken cancellationToken)
    {
        var backendRequest = new { request.PatientId, CorrelationId = (Guid?)null };
        return Forward(_backend.ForwardJsonAsync("triage", HttpMethod.Post, "api/triage/sessions", ForwardContext, backendRequest, cancellationToken), cancellationToken);
    }

    [HttpGet("{sessionId:guid}")]
    public Task<IActionResult> GetSession(Guid sessionId, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("triage", HttpMethod.Get, $"api/triage/sessions/{sessionId}", ForwardContext, cancellationToken: cancellationToken), cancellationToken);

    [HttpPost("{sessionId:guid}/messages")]
    public Task<IActionResult> SendMessage(Guid sessionId, [FromBody] SendTriageMessageRequestDto request, CancellationToken cancellationToken)
    {
        var backendRequest = new { Message = request.Content };
        return Forward(_backend.ForwardJsonAsync("triage", HttpMethod.Post, $"api/triage/sessions/{sessionId}/messages", ForwardContext, backendRequest, cancellationToken), cancellationToken);
    }
}
