using ApiGateway.Application.DTOs.Notifications;
using ApiGateway.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.API.Controllers.V1;

[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public sealed class NotificationsController : GatewayControllerBase
{
    private readonly IBackendForwarder _backend;

    public NotificationsController(IBackendForwarder backend) => _backend = backend;

    [HttpPost("push-tokens")]
    public Task<IActionResult> RegisterPushToken([FromBody] RegisterPushTokenRequestDto request, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardJsonAsync("notification", HttpMethod.Post, "api/notifications/push-tokens", ForwardContext, request, cancellationToken), cancellationToken);

    [HttpGet("preferences")]
    public Task<IActionResult> GetPreferences(CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("notification", HttpMethod.Get, "api/notifications/preferences", ForwardContext, cancellationToken: cancellationToken), cancellationToken);

    [HttpPut("preferences")]
    public Task<IActionResult> UpdatePreference([FromBody] UserPreferenceDto request, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardJsonAsync("notification", HttpMethod.Put, "api/notifications/preferences", ForwardContext, request, cancellationToken), cancellationToken);

    [HttpGet("history")]
    public Task<IActionResult> History([FromQuery] int limit = 50, CancellationToken cancellationToken = default) =>
        Forward(_backend.ForwardAsync("notification", HttpMethod.Get, $"api/notifications/history?limit={limit}", ForwardContext, cancellationToken: cancellationToken), cancellationToken);
}
