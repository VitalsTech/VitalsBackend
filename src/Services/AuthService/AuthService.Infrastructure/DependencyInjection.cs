using AuthService.Application.Interfaces;
using AuthService.Application.Options;
using AuthService.Application.Services;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Clients;
using AuthService.Infrastructure.Data;
using AuthService.Infrastructure.Esia;
using AuthService.Infrastructure.Repositories;
using AuthService.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuthService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<UserServiceOptions>(configuration.GetSection(UserServiceOptions.SectionName));
        services.Configure<EsiaOptions>(configuration.GetSection(EsiaOptions.SectionName));

        services.AddDbContext<AuthDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddSingleton<RsaKeyProvider>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, Argon2PasswordHasher>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();

        services.AddScoped<IAuthUserRepository, AuthUserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();

        services.AddHttpClient<IUserServiceClient, UserServiceClient>();
        services.AddHttpClient<IEsiaOAuthService, EsiaOAuthService>();

        return services;
    }
}
