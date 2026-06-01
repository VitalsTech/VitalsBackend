using AuthService.Application.DTOs;

namespace AuthService.Application.Interfaces;

public interface IAuthenticationService
{
    Task<TokenPairResponse> RegisterAsync(RegisterRequest request, string? ipAddress, string? deviceFingerprint, CancellationToken cancellationToken = default);
    Task<TokenPairResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken = default);
    Task<TokenPairResponse> RefreshAsync(RefreshTokenRequest request, string? ipAddress, CancellationToken cancellationToken = default);
    Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(Guid userPublicId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
    Task<TokenPairResponse> CompleteEsiaLoginAsync(string code, string? ipAddress, string? deviceFingerprint, CancellationToken cancellationToken = default);
    Task LinkEsiaAsync(Guid userPublicId, string code, string currentPassword, CancellationToken cancellationToken = default);
}
