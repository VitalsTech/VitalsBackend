using System.Security.Claims;
using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.API.Controllers;

[ApiController]
[Route("api/auth/esia")]
public class EsiaController : ControllerBase
{
    private readonly IEsiaOAuthService _esia;
    private readonly IAuthenticationService _auth;
    private readonly IEsiaAuthSessionStore _sessions;

    public EsiaController(
        IEsiaOAuthService esia,
        IAuthenticationService auth,
        IEsiaAuthSessionStore sessions)
    {
        _esia = esia;
        _auth = auth;
        _sessions = sessions;
    }

    [HttpGet("config")]
    [AllowAnonymous]
    public ActionResult<EsiaConfigResponse> Config() => Ok(_esia.GetPublicConfig());

    /// <summary>DEV-заглушка: регистрация / вход через Госуслуги (ФИО, почта, телефон).</summary>
    [HttpPost("stub/register")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenPairResponse>> StubRegister(
        [FromBody] EsiaStubRegisterRequest request,
        CancellationToken cancellationToken)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var fingerprint = Request.Headers["X-Device-Fingerprint"].FirstOrDefault();
        return Ok(await _auth.CompleteEsiaStubRegisterAsync(request, ip, fingerprint, cancellationToken));
    }

    /// <summary>DEV-заглушка: привязать Госуслуги к текущему аккаунту (JWT). ОМС и адрес генерируются.</summary>
    [HttpPost("stub/link")]
    [Authorize]
    public async Task<ActionResult<TokenPairResponse>> StubLink(CancellationToken cancellationToken)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        return Ok(await _auth.LinkEsiaStubAsync(ReadUserId(), ip, cancellationToken));
    }

    /// <summary>
    /// Старт OAuth: на DEV со stub не используется. Регистрация — POST stub/register.
    /// </summary>
    [HttpGet("start")]
    [AllowAnonymous]
    public ActionResult<EsiaStartResponse> Start(
        [FromQuery] string intent = "register",
        [FromQuery] string? returnUrl = null,
        [FromQuery] string? responseMode = "fragment")
    {
        if (_esia.UseStub)
            return BadRequest(new
            {
                error = "На DEV Госуслуги работают как заглушка. Регистрация: POST /api/v1/auth/esia/stub/register. Привязка: POST /api/v1/auth/esia/stub/link."
            });

        var normalizedIntent = intent.Equals("link", StringComparison.OrdinalIgnoreCase) ? "link" : "register";
        Guid? userId = null;
        if (normalizedIntent == "link")
        {
            if (User.Identity?.IsAuthenticated != true)
                return Unauthorized(new { error = "Для привязки Госуслуг нужна авторизация." });
            userId = ReadUserId();
        }

        var state = Guid.NewGuid().ToString("N");
        _sessions.Save(new EsiaAuthSession
        {
            State = state,
            Intent = normalizedIntent,
            UserPublicId = userId,
            ReturnUrl = _esia.IsAllowedReturnUrl(returnUrl) ? returnUrl : null,
            ResponseMode = string.Equals(responseMode, "query", StringComparison.OrdinalIgnoreCase) ? "query" : "fragment",
            DeviceFingerprint = Request.Headers["X-Device-Fingerprint"].FirstOrDefault()
        });

        return Ok(new EsiaStartResponse
        {
            AuthorizationUrl = _esia.BuildAuthorizationUrl(state),
            State = state,
            Intent = normalizedIntent,
            Enabled = _esia.GetPublicConfig().Enabled,
            Portal = _esia.GetPublicConfig().Portal
        });
    }

    /// <summary>Совместимость: то же, что start?intent=register.</summary>
    [HttpGet("login")]
    [AllowAnonymous]
    public ActionResult<EsiaStartResponse> Login([FromQuery] string? returnUrl = null, [FromQuery] string? responseMode = "fragment") =>
        Start("register", returnUrl, responseMode);

    [HttpGet("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        [FromQuery] string? error_description,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(error))
            return BadRequest(new { error, error_description });
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
            return BadRequest(new { error = "code and state are required." });

        var session = _sessions.Take(state);
        if (session is null)
            return BadRequest(new { error = "Недействительный или просроченный state Госуслуг. Начните вход заново." });

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var tokens = await _auth.CompleteEsiaSessionAsync(session, code, ip, cancellationToken);
        if (!string.IsNullOrWhiteSpace(session.ReturnUrl))
            return Redirect(BuildReturnUrl(session.ReturnUrl, session.ResponseMode, tokens));

        return Ok(tokens);
    }

    [HttpPost("complete")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenPairResponse>> Complete(
        [FromBody] EsiaCompleteRequest request,
        CancellationToken cancellationToken)
    {
        var session = _sessions.Take(request.State);
        if (session is null)
            return BadRequest(new { error = "Недействительный или просроченный state Госуслуг. Начните вход заново." });
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        return Ok(await _auth.CompleteEsiaSessionAsync(session, request.Code, ip, cancellationToken));
    }

    [HttpGet("status")]
    [Authorize]
    public async Task<ActionResult<EsiaStatusResponse>> Status(CancellationToken cancellationToken) =>
        Ok(await _auth.GetEsiaStatusAsync(ReadUserId(), cancellationToken));

    /// <summary>Привязка по коду (если клиент сам поймал code). JWT обязателен; пароль необязателен.</summary>
    [HttpPost("link")]
    [Authorize]
    public async Task<IActionResult> Link([FromBody] LinkEsiaCallbackRequest request, CancellationToken cancellationToken)
    {
        await _auth.LinkEsiaAsync(ReadUserId(), request.Code, request.CurrentPassword, request.State, cancellationToken);
        return NoContent();
    }

    private Guid ReadUserId() =>
        Guid.Parse(
            User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException());

    private static string BuildReturnUrl(string returnUrl, string responseMode, TokenPairResponse tokens)
    {
        var pairs = new Dictionary<string, string>
        {
            ["access_token"] = tokens.AccessToken,
            ["refresh_token"] = tokens.RefreshToken,
            ["expires_in"] = tokens.ExpiresIn.ToString(),
            ["user_public_id"] = tokens.UserPublicId.ToString(),
            ["esia_linked"] = "true"
        };
        var encoded = string.Join("&", pairs.Select(kv =>
            $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        if (responseMode.Equals("query", StringComparison.OrdinalIgnoreCase))
        {
            var sep = returnUrl.Contains('?') ? "&" : "?";
            return $"{returnUrl}{sep}{encoded}";
        }

        var hashSep = returnUrl.Contains('#') ? "&" : "#";
        return $"{returnUrl}{hashSep}{encoded}";
    }
}

public sealed class LinkEsiaCallbackRequest
{
    public string Code { get; set; } = string.Empty;
    public string? State { get; set; }
    public string? CurrentPassword { get; set; }
}
