using System.Net.Http.Json;
using ApiGateway.Application.DTOs.Common;
using ApiGateway.Application.Interfaces;
using ApiGateway.Application.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ApiGateway.Infrastructure.Clients;

public sealed class BackendForwarder : IBackendForwarder
{
    private readonly IHttpClientFactory _httpClientFactory;

    public BackendForwarder(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    public Task<HttpResponseMessage> ForwardJsonAsync(
        string serviceName,
        HttpMethod method,
        string relativePath,
        BackendForwardContext context,
        object? body = null,
        CancellationToken cancellationToken = default)
    {
        HttpContent? content = body is null ? null : JsonContent.Create(body);
        return ForwardAsync(serviceName, method, relativePath, context, content, cancellationToken);
    }

    public async Task<HttpResponseMessage> ForwardAsync(
        string serviceName,
        HttpMethod method,
        string relativePath,
        BackendForwardContext context,
        HttpContent? content = null,
        CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient(GetClientName(serviceName));
        using var request = new HttpRequestMessage(method, relativePath.TrimStart('/'));

        if (content is not null)
            request.Content = content;

        CopyRequestHeaders(context, request);
        return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    }

    private static void CopyRequestHeaders(BackendForwardContext context, HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(context.Authorization))
            request.Headers.TryAddWithoutValidation("Authorization", context.Authorization);

        if (!string.IsNullOrWhiteSpace(context.RequestId))
            request.Headers.TryAddWithoutValidation("X-Request-ID", context.RequestId);

        if (!string.IsNullOrWhiteSpace(context.DeviceFingerprint))
            request.Headers.TryAddWithoutValidation("X-Device-Fingerprint", context.DeviceFingerprint);

        if (!string.IsNullOrWhiteSpace(context.ClientIp))
            request.Headers.TryAddWithoutValidation("X-Forwarded-For", context.ClientIp);
    }

    private static string GetClientName(string serviceName) => serviceName.ToLowerInvariant() switch
    {
        "auth" => "auth-service",
        "user" => "user-service",
        "medical" => "medical-record-service",
        "triage" => "ai-triage-service",
        "consultation" => "consultation-service",
        "prescription" => "prescription-service",
        "notification" => "notification-service",
        "payment" => "payment-service",
        "analytics" => "analytics-service",
        "quality" => "quality-service",
        _ => throw new ArgumentOutOfRangeException(nameof(serviceName), serviceName, "Unknown backend service.")
    };
}

public static class BackendHttpClientRegistration
{
    public static IHttpClientBuilder AddBackendClient(this IServiceCollection services, string clientName, string baseUrl) =>
        services.AddHttpClient(clientName, client =>
        {
            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(60);
        });
}
