using ApiGateway.API.Infrastructure;
using ApiGateway.Application.DTOs.Auth;
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

    public AuthController(IAuthBackendClient auth) => _auth = auth;

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
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _auth.LoginAsync(request, GetClientIp(), cancellationToken);
        await HttpResponseForwarder.ForwardAsync(HttpContext, response, cancellationToken);
        return new EmptyResult();
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
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
