using System.Net.Http.Json;
using ApiGateway.Application.DTOs.Auth;
using ApiGateway.Application.Interfaces;
using ApiGateway.Application.Options;
using Microsoft.Extensions.Options;

namespace ApiGateway.Infrastructure.Clients;

public sealed class AuthBackendClient : IAuthBackendClient
{
    private readonly HttpClient _http;

    public AuthBackendClient(HttpClient http, IOptions<BackendServicesOptions> options)
    {
        _http = http;
        if (_http.BaseAddress is null)
            _http.BaseAddress = new Uri(options.Value.AuthService.TrimEnd('/') + "/");
    }

    public Task<HttpResponseMessage> RegisterAsync(RegisterRequestDto request, string? clientIp, string? fingerprint, CancellationToken cancellationToken = default) =>
        SendJsonAsync(HttpMethod.Post, "api/auth/register", request, clientIp, fingerprint, null, cancellationToken);

    public Task<HttpResponseMessage> LoginAsync(LoginRequestDto request, string? clientIp, CancellationToken cancellationToken = default) =>
        SendJsonAsync(HttpMethod.Post, "api/auth/login", request, clientIp, request.DeviceFingerprint, null, cancellationToken);

    public Task<HttpResponseMessage> RefreshAsync(RefreshTokenRequestDto request, string? clientIp, CancellationToken cancellationToken = default) =>
        SendJsonAsync(HttpMethod.Post, "api/auth/refresh", request, clientIp, request.DeviceFingerprint, null, cancellationToken);

    public Task<HttpResponseMessage> LogoutAsync(LogoutRequestDto request, CancellationToken cancellationToken = default) =>
        SendJsonAsync(HttpMethod.Post, "api/auth/logout", request, null, null, null, cancellationToken);

    public Task<HttpResponseMessage> ChangePasswordAsync(ChangePasswordRequestDto request, string authorizationHeader, CancellationToken cancellationToken = default) =>
        SendJsonAsync(HttpMethod.Post, "api/auth/change-password", request, null, null, authorizationHeader, cancellationToken);

    public Task<HttpResponseMessage> ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken cancellationToken = default) =>
        SendJsonAsync(HttpMethod.Post, "api/auth/password/forgot", request, null, null, null, cancellationToken);

    public Task<HttpResponseMessage> ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken = default) =>
        SendJsonAsync(HttpMethod.Post, "api/auth/password/reset", request, null, null, null, cancellationToken);

    public Task<HttpResponseMessage> ForwardEsiaAsync(HttpRequestMessage request, CancellationToken cancellationToken = default) =>
        _http.SendAsync(request, cancellationToken);

    private async Task<HttpResponseMessage> SendJsonAsync(
        HttpMethod method,
        string path,
        object body,
        string? clientIp,
        string? fingerprint,
        string? authorization,
        CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(method, path) { Content = JsonContent.Create(body) };
        if (!string.IsNullOrEmpty(clientIp))
            message.Headers.TryAddWithoutValidation("X-Forwarded-For", clientIp);
        if (!string.IsNullOrEmpty(fingerprint))
            message.Headers.TryAddWithoutValidation("X-Device-Fingerprint", fingerprint);
        if (!string.IsNullOrEmpty(authorization))
            message.Headers.TryAddWithoutValidation("Authorization", authorization);
        return await _http.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    }
}
