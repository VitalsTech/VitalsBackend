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

        var rateLimit = configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>()
            ?? new RateLimitingOptions();

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(rateLimit.RedisConnectionString));

        services.AddSingleton<IRateLimitStore, RedisSlidingWindowRateLimitStore>();
        services.AddSingleton<RateLimitPolicyResolver>();
        services.AddSingleton<JwtSigningKeyProvider>();

        services.AddHttpClient("jwks");
        services.AddHttpClient<IAuthBackendClient, AuthBackendClient>();

        return services;
    }
}
