using System.Net.Http.Json;
using NotificationService.Application.Interfaces;
using NotificationService.Application.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vitals.AspNetCore.Authentication;

namespace NotificationService.Infrastructure.Clients;

public sealed class IntegrationChannelClient : IIntegrationChannelClient
{
    private readonly HttpClient _http;
    private readonly IntegrationServiceOptions _integration;
    private readonly ILogger<IntegrationChannelClient> _logger;

    public IntegrationChannelClient(
        HttpClient http,
        IOptions<IntegrationServiceOptions> integration,
        ILogger<IntegrationChannelClient> logger)
    {
        _http = http;
        _integration = integration.Value;
        _logger = logger;

        if (_http.BaseAddress is null && !string.IsNullOrWhiteSpace(_integration.BaseUrl))
            _http.BaseAddress = new Uri(_integration.BaseUrl.TrimEnd('/') + "/");
    }

    public async Task<string?> SendSmsAsync(Guid userId, string body, CancellationToken cancellationToken = default)
    {
        if (_integration.UseStub)
            return null;

        var response = await _http.PostAsJsonAsync(
            "internal/integration/sms/send",
            new { UserId = userId, Phone = string.Empty, Body = body },
            cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Integration SMS failed for user {UserId}: {Status}", userId, response.StatusCode);
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<IntegrationDispatchResponse>(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return payload?.ExternalReference;
    }

    public async Task<string?> SendEmailAsync(
        Guid userId,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        if (_integration.UseStub)
            return null;

        var response = await _http.PostAsJsonAsync(
            "internal/integration/email/send",
            new { UserId = userId, Email = string.Empty, Subject = subject, HtmlBody = body },
            cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Integration email failed for user {UserId}: {Status}", userId, response.StatusCode);
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<IntegrationDispatchResponse>(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return payload?.ExternalReference;
    }

    public async Task<string?> SendPushAsync(
        Guid userId,
        string platform,
        string title,
        string body,
        CancellationToken cancellationToken = default)
    {
        if (_integration.UseStub)
            return null;

        var response = await _http.PostAsJsonAsync(
            "internal/integration/push/send",
            new { UserId = userId, Platform = platform, Title = title, Body = body },
            cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Integration push failed for user {UserId}: {Status}", userId, response.StatusCode);
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<IntegrationDispatchResponse>(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return payload?.ExternalReference;
    }

    public async Task<string?> SendVoiceAsync(
        Guid userId,
        string message,
        int urgencyLevel,
        CancellationToken cancellationToken = default)
    {
        if (_integration.UseStub)
            return null;

        var response = await _http.PostAsJsonAsync(
            "internal/integration/voice/call",
            new { UserId = userId, Phone = string.Empty, Message = message, UrgencyLevel = urgencyLevel },
            cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Integration voice call failed for user {UserId}: {Status}", userId, response.StatusCode);
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<IntegrationDispatchResponse>(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return payload?.ExternalReference;
    }

    private sealed class IntegrationDispatchResponse
    {
        public string? ExternalReference { get; set; }
    }
}

public static class IntegrationChannelClientRegistration
{
    public static IHttpClientBuilder AddIntegrationChannelClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ServiceAuthOptions>(configuration.GetSection(ServiceAuthOptions.SectionName));

        return services.AddHttpClient<IIntegrationChannelClient, IntegrationChannelClient>((sp, client) =>
        {
            var integration = sp.GetRequiredService<IOptions<IntegrationServiceOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(integration.BaseUrl))
                client.BaseAddress = new Uri(integration.BaseUrl.TrimEnd('/') + "/");

            var serviceAuth = sp.GetRequiredService<IOptions<ServiceAuthOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(serviceAuth.ApiKey))
            {
                client.DefaultRequestHeaders.Add("X-Service-Key", serviceAuth.ApiKey);
                client.DefaultRequestHeaders.Add("X-Service-Name", "notification-service");
            }
        });
    }
}
