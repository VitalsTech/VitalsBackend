using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ApiGateway.Application.Options;
using ApiGateway.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ApiGateway.API.Middleware;

/// <summary>
/// Optional JWT validation: populates HttpContext.User when token is valid; anonymous otherwise.
/// </summary>
public sealed class JwtAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly JwtOptions _jwtOptions;
    private readonly JwtSigningKeyProvider _keyProvider;
    private readonly ILogger<JwtAuthenticationMiddleware> _logger;

    public JwtAuthenticationMiddleware(
        RequestDelegate next,
        IOptions<JwtOptions> jwtOptions,
        JwtSigningKeyProvider keyProvider,
        ILogger<JwtAuthenticationMiddleware> logger)
    {
        _next = next;
        _jwtOptions = jwtOptions.Value;
        _keyProvider = keyProvider;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var authorization = context.Request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(authorization) &&
            authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authorization["Bearer ".Length..].Trim();
            var principal = ValidateToken(token);
            if (principal is not null)
                context.User = principal;
        }

        await _next(context);
    }

    private ClaimsPrincipal? ValidateToken(string token)
    {
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = _jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _keyProvider.SigningKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        try
        {
            var handler = new JwtSecurityTokenHandler();
            return handler.ValidateToken(token, parameters, out _);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "JWT validation failed at gateway");
            return null;
        }
    }
}
