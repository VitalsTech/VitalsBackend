using ApiGateway.API.Infrastructure;
using ApiGateway.Application.DTOs.Auth;
using ApiGateway.Application.DTOs.Common;
using ApiGateway.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.API.Controllers.V1;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthBackendClient _auth;
    private readonly IBackendForwarder _backend;

    public AuthController(IAuthBackendClient auth, IBackendForwarder backend)
    {
        _auth = auth;
        _backend = backend;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _auth.RegisterAsync(request, GetClientIp(), GetFingerprint(), cancellationToken);
        await HttpResponseForwarder.ForwardAsync(HttpContext, response, cancellationToken);
        return new EmptyResult();
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenPairResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _auth.LoginAsync(request, GetClientIp(), cancellationToken);
        await HttpResponseForwarder.ForwardAsync(HttpContext, response, cancellationToken);
        return new EmptyResult();
    }

    [HttpPost("switch-profile")]
    [Authorize]
    [ProducesResponseType(typeof(TokenPairResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SwitchProfile([FromBody] Application.DTOs.Users.SwitchProfileRequestDto request, CancellationToken cancellationToken)
    {
        var body = new
        {
            profileId = request.ProfileId,
            refreshToken = request.RefreshToken,
            deviceFingerprint = request.DeviceFingerprint ?? GetFingerprint()
        };
        var response = await _backend.ForwardJsonAsync(
            "auth",
            HttpMethod.Post,
            "api/auth/switch-profile",
            new BackendForwardContext
            {
                Authorization = Request.Headers.Authorization.ToString(),
                ClientIp = GetClientIp(),
                DeviceFingerprint = GetFingerprint(),
                RequestId = HttpContext.TraceIdentifier
            },
            body,
            cancellationToken);
        await HttpResponseForwarder.ForwardAsync(HttpContext, response, cancellationToken);
        return new EmptyResult();
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenPairResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _auth.RefreshAsync(request, GetClientIp(), cancellationToken);
        await HttpResponseForwarder.ForwardAsync(HttpContext, response, cancellationToken);
        return new EmptyResult();
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout([FromBody] LogoutRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _auth.LogoutAsync(request, cancellationToken);
        await HttpResponseForwarder.ForwardAsync(HttpContext, response, cancellationToken);
        return new EmptyResult();
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _auth.ChangePasswordAsync(request, Request.Headers.Authorization.ToString(), cancellationToken);
        await HttpResponseForwarder.ForwardAsync(HttpContext, response, cancellationToken);
        return new EmptyResult();
    }

    [HttpPost("password/forgot")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _auth.ForgotPasswordAsync(request, cancellationToken);
        await HttpResponseForwarder.ForwardAsync(HttpContext, response, cancellationToken);
        return new EmptyResult();
    }

    [HttpPost("password/reset")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _auth.ResetPasswordAsync(request, cancellationToken);
        await HttpResponseForwarder.ForwardAsync(HttpContext, response, cancellationToken);
        return new EmptyResult();
    }

    private string? GetClientIp() =>
        HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? GetFingerprint() =>
        Request.Headers["X-Device-Fingerprint"].FirstOrDefault();
}
