using AuthService.Application.DTOs;

namespace AuthService.Application.Interfaces;

public interface IEsiaOAuthService
{
    bool UseStub { get; }
    bool IsAvailable { get; }
    EsiaConfigResponse GetPublicConfig();
    bool IsAllowedReturnUrl(string? returnUrl);
    string BuildAuthorizationUrl(string state);
    Task<EsiaUserInfo> ExchangeCodeAsync(string code, string? state = null, CancellationToken cancellationToken = default);
}
