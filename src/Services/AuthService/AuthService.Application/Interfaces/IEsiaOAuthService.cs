using AuthService.Application.DTOs;

namespace AuthService.Application.Interfaces;

public interface IEsiaOAuthService
{
    string BuildAuthorizationUrl(string state);
    Task<EsiaUserInfo> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default);
}
