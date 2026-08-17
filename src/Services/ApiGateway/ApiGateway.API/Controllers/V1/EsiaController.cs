using ApiGateway.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.API.Controllers.V1;

[ApiController]
[Route("api/v1/auth/esia")]
public sealed class EsiaController : GatewayControllerBase
{
    private readonly IBackendForwarder _backend;

    public EsiaController(IBackendForwarder backend) => _backend = backend;

    [HttpGet("config")]
    [AllowAnonymous]
    public Task<IActionResult> Config(CancellationToken cancellationToken) =>
        ForwardEsia("api/auth/esia/config", HttpMethod.Get, cancellationToken);

    [HttpGet("start")]
    [AllowAnonymous]
    public Task<IActionResult> Start(CancellationToken cancellationToken) =>
        ForwardEsia($"api/auth/esia/start{Request.QueryString}", HttpMethod.Get, cancellationToken);

    [HttpGet("login")]
    [AllowAnonymous]
    public Task<IActionResult> Login(CancellationToken cancellationToken) =>
        ForwardEsia($"api/auth/esia/login{Request.QueryString}", HttpMethod.Get, cancellationToken);

    [HttpGet("callback")]
    [AllowAnonymous]
    public Task<IActionResult> Callback(CancellationToken cancellationToken) =>
        ForwardEsia($"api/auth/esia/callback{Request.QueryString}", HttpMethod.Get, cancellationToken);

    [HttpGet("status")]
    [Authorize]
    public Task<IActionResult> Status(CancellationToken cancellationToken) =>
        ForwardEsia("api/auth/esia/status", HttpMethod.Get, cancellationToken);

    [HttpPost("complete")]
    [AllowAnonymous]
    public Task<IActionResult> Complete([FromBody] object body, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardJsonAsync("auth", HttpMethod.Post, "api/auth/esia/complete", ForwardContext, body, cancellationToken), cancellationToken);

    [HttpPost("stub/register")]
    [AllowAnonymous]
    public Task<IActionResult> StubRegister([FromBody] object body, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardJsonAsync("auth", HttpMethod.Post, "api/auth/esia/stub/register", ForwardContext, body, cancellationToken), cancellationToken);

    [HttpPost("stub/link")]
    [Authorize]
    public Task<IActionResult> StubLink(CancellationToken cancellationToken) =>
        Forward(_backend.ForwardJsonAsync("auth", HttpMethod.Post, "api/auth/esia/stub/link", ForwardContext, new { }, cancellationToken), cancellationToken);

    [HttpPost("link")]
    [Authorize]
    public Task<IActionResult> Link([FromBody] object body, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardJsonAsync("auth", HttpMethod.Post, "api/auth/esia/link", ForwardContext, body, cancellationToken), cancellationToken);

    private Task<IActionResult> ForwardEsia(string path, HttpMethod method, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("auth", method, path, ForwardContext, cancellationToken: cancellationToken), cancellationToken);
}
