using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.API.Controllers;

[ApiController]
[Route("internal")]
public class InternalController : ControllerBase
{
    private readonly IJwtTokenService _jwtTokenService;

    public InternalController(IJwtTokenService jwtTokenService) => _jwtTokenService = jwtTokenService;

    [HttpPost("token/validate")]
    public ActionResult<ValidateTokenResponse> ValidateToken([FromBody] ValidateTokenRequest request)
    {
        var result = _jwtTokenService.ValidateAccessToken(request.Token);
        return Ok(result);
    }

    [HttpGet("jwks")]
    public ContentResult GetJwks() => Content(_jwtTokenService.GetJwksJson(), "application/json");

    [HttpGet(".well-known/jwks.json")]
    public ContentResult GetWellKnownJwks() => Content(_jwtTokenService.GetJwksJson(), "application/json");
}
