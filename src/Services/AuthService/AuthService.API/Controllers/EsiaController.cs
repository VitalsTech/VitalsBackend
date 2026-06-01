using System.Security.Claims;
using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using AuthService.Application.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AuthService.API.Controllers;

[ApiController]
[Route("api/auth/esia")]
public class EsiaController : ControllerBase
{
    private readonly IEsiaOAuthService _esia;
    private readonly IAuthenticationService _auth;
    private readonly EsiaOptions _esiaOptions;

    public EsiaController(
        IEsiaOAuthService esia,
        IAuthenticationService auth,
        IOptions<EsiaOptions> esiaOptions)
    {
        _esia = esia;
        _auth = auth;
        _esiaOptions = esiaOptions.Value;
    }

    /// <summary>
    /// Возвращает URL для редиректа на портал Госуслуг (шаг 2 сценария ЕСИА).
    /// </summary>
    [HttpGet("login")]
    [AllowAnonymous]
    public ActionResult<EsiaLoginResponse> Login()
    {
        var state = Guid.NewGuid().ToString("N");
        HttpContext.Session.SetString("esia_state", state);
        var url = _esia.BuildAuthorizationUrl(state);
        return Ok(new EsiaLoginResponse { AuthorizationUrl = url, State = state });
    }

    /// <summary>
    /// Callback после авторизации на ЕСИА (шаг 4–7).
    /// </summary>
    [HttpGet("callback")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenPairResponse>> Callback(
        [FromQuery] string code,
        [FromQuery] string state,
        CancellationToken cancellationToken)
    {
        var expectedState = HttpContext.Session.GetString("esia_state");
        if (string.IsNullOrEmpty(expectedState) || expectedState != state)
            return BadRequest(new { error = "Invalid ESIA state." });

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var fingerprint = HttpContext.Request.Headers["X-Device-Fingerprint"].FirstOrDefault();
        var tokens = await _auth.CompleteEsiaLoginAsync(code, ip, fingerprint, cancellationToken);
        return Ok(tokens);
    }

    /// <summary>
    /// Привязка ЕСИА к существующему аккаунту (телефон + пароль).
    /// </summary>
    [HttpPost("link")]
    [Authorize]
    public async Task<IActionResult> Link([FromBody] LinkEsiaCallbackRequest request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException());

        await _auth.LinkEsiaAsync(userId, request.Code, request.CurrentPassword, cancellationToken);
        return NoContent();
    }

    [HttpGet("config")]
    [AllowAnonymous]
    public ActionResult<object> Config() =>
        Ok(new { enabled = _esiaOptions.Enabled, redirectUri = _esiaOptions.RedirectUri });
}

public sealed class EsiaLoginResponse
{
    public string AuthorizationUrl { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}

public sealed class LinkEsiaCallbackRequest
{
    public string Code { get; set; } = string.Empty;
    public string CurrentPassword { get; set; } = string.Empty;
}
