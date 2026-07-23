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
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetUser(Guid publicId, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("user", HttpMethod.Get, $"api/users/{publicId}", ForwardContext, cancellationToken: cancellationToken), cancellationToken);

    [HttpGet("{publicId:guid}/profiles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IActionResult> GetProfiles(Guid publicId, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("user", HttpMethod.Get, $"api/users/{publicId}/profiles", ForwardContext, cancellationToken: cancellationToken), cancellationToken);

    /// <summary>
    /// Self-service: только профиль Patient. Doctor/Organization — через регистрацию или admin.
    /// </summary>
    [HttpPost("add-profile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> AddProfile([FromBody] Application.DTOs.Users.AddProfileRequestDto request, CancellationToken cancellationToken)
    {
        if (!string.Equals(request.ProfileType, "Patient", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult<IActionResult>(BadRequest(new
            {
                error = "Only Patient profile can be added via self-service. Use registration or an admin API for Doctor/Organization."
            }));

        var backendRequest = new
        {
            userPublicId = request.PublicId,
            profileType = request.ProfileType,
            patientProfile = request.PatientProfile
        };
        return Forward(_backend.ForwardJsonAsync("user", HttpMethod.Post, "api/users/add-profile", ForwardContext, backendRequest, cancellationToken), cancellationToken);
    }

    /// <summary>
    /// Переключает активный профиль и выпускает новый JWT с ролями этого профиля.
    /// </summary>
    [HttpPost("switch-profile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IActionResult> SwitchProfile([FromBody] Application.DTOs.Users.SwitchProfileRequestDto request, CancellationToken cancellationToken)
    {
        var backendRequest = new
        {
            profileId = request.ProfileId,
            refreshToken = request.RefreshToken,
            deviceFingerprint = request.DeviceFingerprint
        };
        return Forward(_backend.ForwardJsonAsync("auth", HttpMethod.Post, "api/auth/switch-profile", ForwardContext, backendRequest, cancellationToken), cancellationToken);
    }

    [HttpGet("{publicId:guid}/has-profile/{profileType}")]
    public Task<IActionResult> HasProfile(Guid publicId, string profileType, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("user", HttpMethod.Get, $"api/users/{publicId}/has-profile/{profileType}", ForwardContext, cancellationToken: cancellationToken), cancellationToken);
}
