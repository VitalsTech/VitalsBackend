using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using AuthService.Application.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Infrastructure.Security;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;
    private readonly RsaKeyProvider _keyProvider;
    private readonly JwtSecurityTokenHandler _handler = new();

    public JwtTokenService(IOptions<JwtOptions> options, RsaKeyProvider keyProvider)
    {
        _options = options.Value;
        _keyProvider = keyProvider;
    }

    public string CreateAccessToken(
        Guid userPublicId,
        IEnumerable<string> roles,
        IEnumerable<string> scopes,
        IEnumerable<Guid>? profileIds = null)
    {
        var now = DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userPublicId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(JwtRegisteredClaimNames.Iat, EpochSeconds(now).ToString(), ClaimValueTypes.Integer64)
        };

        claims.AddRange(roles.Distinct().SelectMany(r => new[]
        {
            new Claim("role", r),
            // Dual claim for handlers that still map inbound claim types.
            new Claim(ClaimTypes.Role, r)
        }));
        claims.AddRange(scopes.Distinct().Select(s => new Claim("scope", s)));
        if (profileIds is not null)
            claims.AddRange(profileIds.Distinct().Select(id => new Claim("profile_id", id.ToString())));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = now.AddMinutes(_options.AccessTokenMinutes),
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            SigningCredentials = new SigningCredentials(_keyProvider.SigningKey, SecurityAlgorithms.RsaSha256)
        };

        return _handler.WriteToken(_handler.CreateToken(descriptor));
    }

    public ValidateTokenResponse ValidateAccessToken(string token)
    {
        try
        {
            var principal = _handler.ValidateToken(token, GetValidationParameters(), out var validated);
            var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(sub, out var userPublicId))
                return Invalid("Invalid subject claim.");

            return new ValidateTokenResponse
            {
                IsValid = true,
                UserPublicId = userPublicId,
                Roles = principal.FindAll(ClaimTypes.Role)
                    .Concat(principal.FindAll("role"))
                    .Select(c => c.Value)
                    .Distinct()
                    .ToList(),
                Scopes = principal.FindAll("scope").Select(c => c.Value).Distinct().ToList(),
                ExpiresAt = validated.ValidTo
            };
        }
        catch (Exception ex)
        {
            return Invalid(ex.Message);
        }
    }

    public string GetJwksJson()
    {
        var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(_keyProvider.SigningKey);
        jwk.Use = "sig";
        jwk.Alg = SecurityAlgorithms.RsaSha256;
        var set = new JsonWebKeySet { Keys = { jwk } };
        return JsonSerializer.Serialize(set);
    }

    private TokenValidationParameters GetValidationParameters() => new()
    {
        ValidateIssuer = true,
        ValidIssuer = _options.Issuer,
        ValidateAudience = true,
        ValidAudience = _options.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = _keyProvider.SigningKey,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30),
        NameClaimType = "sub",
        RoleClaimType = "role"
    };

    private static ValidateTokenResponse Invalid(string reason) =>
        new() { IsValid = false, Reason = reason };

    private static long EpochSeconds(DateTime utc) =>
        new DateTimeOffset(utc).ToUnixTimeSeconds();
}
