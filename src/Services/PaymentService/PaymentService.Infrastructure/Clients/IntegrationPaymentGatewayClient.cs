using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PaymentService.Application.Interfaces;
using PaymentService.Application.Options;
using PaymentService.Domain.Entities;
using Vitals.AspNetCore.Authentication;

namespace PaymentService.Infrastructure.Clients;

public sealed class IntegrationPaymentGatewayClient : IPaymentGatewayClient
{
    private readonly HttpClient _http;
    private readonly ILogger<IntegrationPaymentGatewayClient> _logger;

    public IntegrationPaymentGatewayClient(HttpClient http, ILogger<IntegrationPaymentGatewayClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<PaymentGatewayResult> ProcessPaymentAsync(PaymentTransaction payment, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync("internal/integration/payments/process", new
        {
            payment.Id,
            payment.UserId,
            payment.Amount,
            payment.Currency,
            payment.PaymentMethodToken
        }, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogWarning("Payment gateway failed: {Status} {Body}", response.StatusCode, body);
            return new PaymentGatewayResult { Success = false, FailureReason = body };
        }

        var payload = await response.Content.ReadFromJsonAsync<GatewayResponse>(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return new PaymentGatewayResult
        {
            Success = payload?.Status is "completed" or "accepted",
            ExternalReference = payload?.ExternalReference,
            FailureReason = payload?.FailureReason
        };
    }

    private sealed class GatewayResponse
    {
        public string? Status { get; set; }
        public string? ExternalReference { get; set; }
        public string? FailureReason { get; set; }
    }
}

public static class IntegrationPaymentGatewayRegistration
{
    public static IHttpClientBuilder AddIntegrationPaymentGateway(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<IntegrationServiceOptions>(configuration.GetSection(IntegrationServiceOptions.SectionName));
        services.Configure<ServiceAuthOptions>(configuration.GetSection(ServiceAuthOptions.SectionName));

        return services.AddHttpClient<IPaymentGatewayClient, IntegrationPaymentGatewayClient>((sp, client) =>
        {
            var integration = sp.GetRequiredService<IOptions<IntegrationServiceOptions>>().Value;
            client.BaseAddress = new Uri(integration.BaseUrl.TrimEnd('/') + "/");

            var serviceAuth = sp.GetRequiredService<IOptions<ServiceAuthOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(serviceAuth.ApiKey))
            {
                client.DefaultRequestHeaders.Add("X-Service-Key", serviceAuth.ApiKey);
                client.DefaultRequestHeaders.Add("X-Service-Name", "payment-service");
            }
        });
    }
}
