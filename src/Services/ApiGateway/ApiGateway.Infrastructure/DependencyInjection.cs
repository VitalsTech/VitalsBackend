using ApiGateway.Application.Interfaces;
using ApiGateway.Application.Options;
using ApiGateway.Application.RateLimiting;
using ApiGateway.Infrastructure.Clients;
using ApiGateway.Infrastructure.RateLimiting;
using ApiGateway.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace ApiGateway.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddGatewayInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<BackendServicesOptions>(configuration.GetSection(BackendServicesOptions.SectionName));
        services.Configure<RateLimitingOptions>(configuration.GetSection(RateLimitingOptions.SectionName));

        var servicesOptions = configuration.GetSection(BackendServicesOptions.SectionName).Get<BackendServicesOptions>()
            ?? new BackendServicesOptions();
        var rateLimit = configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>()
            ?? new RateLimitingOptions();

        RegisterRateLimiting(services, rateLimit);

        services.AddSingleton<JwtSigningKeyProvider>();

        services.AddHttpClient("jwks");
        services.AddHttpClient<IAuthBackendClient, AuthBackendClient>();
        services.AddSingleton<IBackendForwarder, BackendForwarder>();

        BackendHttpClientRegistration.AddBackendClient(services, "auth-service", servicesOptions.AuthService);
        BackendHttpClientRegistration.AddBackendClient(services, "user-service", servicesOptions.UserService);
        BackendHttpClientRegistration.AddBackendClient(services, "medical-record-service", servicesOptions.MedicalRecordService);
        BackendHttpClientRegistration.AddBackendClient(services, "ai-triage-service", servicesOptions.AITriageService);
        BackendHttpClientRegistration.AddBackendClient(services, "consultation-service", servicesOptions.ConsultationService);
        BackendHttpClientRegistration.AddBackendClient(services, "prescription-service", servicesOptions.PrescriptionService);
        BackendHttpClientRegistration.AddBackendClient(services, "notification-service", servicesOptions.NotificationService);
        BackendHttpClientRegistration.AddBackendClient(services, "payment-service", servicesOptions.PaymentService);
        BackendHttpClientRegistration.AddBackendClient(services, "analytics-service", servicesOptions.AnalyticsService);
        BackendHttpClientRegistration.AddBackendClient(services, "quality-service", servicesOptions.QualityService);

        return services;
    }

    private static void RegisterRateLimiting(IServiceCollection services, RateLimitingOptions rateLimit)
    {
        services.AddSingleton<RateLimitPolicyResolver>();

        if (rateLimit.UseInMemoryFallback)
        {
            services.AddSingleton<IRateLimitStore, InMemorySlidingWindowRateLimitStore>();
            return;
        }

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(rateLimit.RedisConnectionString));
        services.AddSingleton<IRateLimitStore, RedisSlidingWindowRateLimitStore>();
    }
}
