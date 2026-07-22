using AuthService.Application.DTOs;

namespace AuthService.Application.Interfaces;

public interface IJwtTokenService
{
    string CreateAccessToken(
        Guid userPublicId,
        IEnumerable<string> roles,
        IEnumerable<string> scopes,
        IEnumerable<Guid>? profileIds = null);
    ValidateTokenResponse ValidateAccessToken(string token);
    string GetJwksJson();
}
