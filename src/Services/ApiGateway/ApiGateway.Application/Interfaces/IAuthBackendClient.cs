using ApiGateway.Application.DTOs.Auth;

namespace ApiGateway.Application.Interfaces;

public interface IAuthBackendClient
{
    Task<HttpResponseMessage> RegisterAsync(RegisterRequestDto request, string? clientIp, string? fingerprint, CancellationToken cancellationToken = default);
    Task<HttpResponseMessage> LoginAsync(LoginRequestDto request, string? clientIp, CancellationToken cancellationToken = default);
    Task<HttpResponseMessage> RefreshAsync(RefreshTokenRequestDto request, string? clientIp, CancellationToken cancellationToken = default);
    Task<HttpResponseMessage> LogoutAsync(LogoutRequestDto request, CancellationToken cancellationToken = default);
    Task<HttpResponseMessage> ChangePasswordAsync(ChangePasswordRequestDto request, string authorizationHeader, CancellationToken cancellationToken = default);
    Task<HttpResponseMessage> ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken cancellationToken = default);
    Task<HttpResponseMessage> ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken = default);
    Task<HttpResponseMessage> ForwardEsiaAsync(HttpRequestMessage request, CancellationToken cancellationToken = default);
}
