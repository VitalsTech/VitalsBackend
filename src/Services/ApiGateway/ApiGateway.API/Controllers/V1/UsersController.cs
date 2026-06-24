using ApiGateway.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.API.Controllers.V1;

[ApiController]
[Route("api/v1/users")]
[Authorize]
public sealed class UsersController : GatewayControllerBase
{
    private readonly IBackendForwarder _backend;

    public UsersController(IBackendForwarder backend) => _backend = backend;

    [HttpGet("{publicId:guid}")]
    public Task<IActionResult> GetUser(Guid publicId, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("user", HttpMethod.Get, $"api/users/{publicId}", ForwardContext, cancellationToken: cancellationToken), cancellationToken);

    [HttpGet("{publicId:guid}/profiles")]
    public Task<IActionResult> GetProfiles(Guid publicId, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("user", HttpMethod.Get, $"api/users/{publicId}/profiles", ForwardContext, cancellationToken: cancellationToken), cancellationToken);

    [HttpPost("add-profile")]
    public Task<IActionResult> AddProfile([FromBody] Application.DTOs.Users.AddProfileRequestDto request, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardJsonAsync("user", HttpMethod.Post, "api/users/add-profile", ForwardContext, request, cancellationToken), cancellationToken);

    [HttpPost("switch-profile")]
    public Task<IActionResult> SwitchProfile([FromBody] Application.DTOs.Users.SwitchProfileRequestDto request, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardJsonAsync("user", HttpMethod.Post, "api/users/switch-profile", ForwardContext, request, cancellationToken), cancellationToken);

    [HttpGet("{publicId:guid}/has-profile/{profileType}")]
    public Task<IActionResult> HasProfile(Guid publicId, string profileType, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("user", HttpMethod.Get, $"api/users/{publicId}/has-profile/{profileType}", ForwardContext, cancellationToken: cancellationToken), cancellationToken);
}
