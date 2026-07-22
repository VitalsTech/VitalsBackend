using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Vitals.AspNetCore.Authentication;

public static class VitalsAuthenticationExtensions
{
    public const string ServiceKeyScheme = "ServiceKey";

    public static IServiceCollection AddVitalsAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.Configure<JwtValidationOptions>(configuration.GetSection(JwtValidationOptions.SectionName));
        services.Configure<ServiceAuthOptions>(configuration.GetSection(ServiceAuthOptions.SectionName));
        services.AddHttpClient(nameof(JwksSigningKeyProvider));
        services.AddSingleton<JwksSigningKeyProvider>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer()
            .AddScheme<AuthenticationSchemeOptions, ServiceApiKeyAuthenticationHandler>(ServiceKeyScheme, _ => { });

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<JwksSigningKeyProvider, IOptions<JwtValidationOptions>>((options, jwks, jwtOptions) =>
            {
                ConfigureJwtBearer(options, jwks, jwtOptions.Value, environment);
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("InternalService", policy =>
            {
                policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme, ServiceKeyScheme);
                policy.RequireAuthenticatedUser();
            });
        });

        return services;
    }

    public static IApplicationBuilder UseVitalsInternalServiceAuth(this IApplicationBuilder app) =>
        app.UseMiddleware<InternalServiceAuthMiddleware>();

    private static void ConfigureJwtBearer(
        JwtBearerOptions options,
        JwksSigningKeyProvider jwksProvider,
        JwtValidationOptions jwt,
        IHostEnvironment environment)
    {
        if (!string.IsNullOrWhiteSpace(jwt.RsaPublicKeyPem))
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(jwt.RsaPublicKeyPem);
            options.TokenValidationParameters = CreateParameters(jwt, new RsaSecurityKey(rsa));
        }
        else if (!string.IsNullOrWhiteSpace(jwt.JwksUrl))
        {
            options.TokenValidationParameters = CreateParameters(jwt, jwksProvider.GetSigningKey());
        }
        else if (environment.IsDevelopment() && jwt.AllowInsecureDevelopmentBypass)
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateIssuerSigningKey = false,
                ValidateLifetime = false,
                SignatureValidator = (token, _) => new Microsoft.IdentityModel.JsonWebTokens.JsonWebToken(token)
            };
        }
        else
        {
            throw new InvalidOperationException(
                "Configure Jwt:JwksUrl or Jwt:RsaPublicKeyPem, or set Jwt:AllowInsecureDevelopmentBypass=true only in local Development.");
        }

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Path.StartsWithSegments("/internal") &&
                    jwt.AllowDevelopmentHeaderFallback &&
                    environment.IsDevelopment() &&
                    context.Request.Headers.ContainsKey("X-Service-Name"))
                {
                    context.Principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim("sub", context.Request.Headers["X-User-Id"].FirstOrDefault() ?? Guid.Empty.ToString()),
                        new Claim(ClaimTypes.Role, "Service")
                    }, "Development"));
                    context.Success();
                }

                return Task.CompletedTask;
            }
        };
    }

    private static TokenValidationParameters CreateParameters(JwtValidationOptions jwt, SecurityKey key) => new()
    {
        ValidateIssuer = true,
        ValidIssuer = jwt.Issuer,
        ValidateAudience = true,
        ValidAudience = jwt.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = key,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30),
        NameClaimType = "sub",
        // AuthService serializes roles as short JWT claim "role" (outbound claim map).
        RoleClaimType = "role"
    };
}

public sealed class JwksSigningKeyProvider
{
    private readonly JwtValidationOptions _jwt;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<JwksSigningKeyProvider> _logger;
    private SecurityKey? _cachedKey;
    private DateTime _cachedAt;

    public JwksSigningKeyProvider(
        IOptions<JwtValidationOptions> jwt,
        IHttpClientFactory httpClientFactory,
        ILogger<JwksSigningKeyProvider> logger)
    {
        _jwt = jwt.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public SecurityKey GetSigningKey()
    {
        if (_cachedKey is not null && DateTime.UtcNow - _cachedAt < TimeSpan.FromMinutes(5))
            return _cachedKey;

        if (string.IsNullOrWhiteSpace(_jwt.JwksUrl))
            throw new InvalidOperationException("Jwt:JwksUrl is not configured.");

        var client = _httpClientFactory.CreateClient(nameof(JwksSigningKeyProvider));
        var document = client.GetFromJsonAsync<JsonWebKeySetDocument>(_jwt.JwksUrl).GetAwaiter().GetResult()
            ?? throw new InvalidOperationException($"JWKS document at {_jwt.JwksUrl} is empty.");

        var jwk = document.Keys?.FirstOrDefault(k => k.Kty == "RSA" && !string.IsNullOrWhiteSpace(k.N))
            ?? throw new InvalidOperationException("JWKS does not contain an RSA key.");

        var rsa = RSA.Create();
        rsa.ImportParameters(new RSAParameters
        {
            Modulus = Base64UrlEncoder.DecodeBytes(jwk.N!),
            Exponent = Base64UrlEncoder.DecodeBytes(jwk.E ?? "AQAB")
        });

        _cachedKey = new RsaSecurityKey(rsa);
        _cachedAt = DateTime.UtcNow;
        _logger.LogInformation("Loaded JWKS signing key from {Url}", _jwt.JwksUrl);
        return _cachedKey;
    }

    private sealed class JsonWebKeySetDocument
    {
        public List<JsonWebKeyDocument>? Keys { get; set; }
    }

    private sealed class JsonWebKeyDocument
    {
        public string? Kty { get; set; }
        public string? N { get; set; }
        public string? E { get; set; }
    }
}

public sealed class ServiceApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly ServiceAuthOptions _serviceAuth;

    public ServiceApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder,
        IOptions<ServiceAuthOptions> serviceAuth)
        : base(options, logger, encoder) =>
        _serviceAuth = serviceAuth.Value;

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (string.IsNullOrWhiteSpace(_serviceAuth.ApiKey))
            return Task.FromResult(AuthenticateResult.NoResult());

        if (!Request.Headers.TryGetValue("X-Service-Key", out var provided) ||
            !string.Equals(provided.ToString(), _serviceAuth.ApiKey, StringComparison.Ordinal))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid service key."));
        }

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, Request.Headers["X-Service-Name"].FirstOrDefault() ?? "internal-service"),
            new Claim(ClaimTypes.Role, "Service")
        }, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}

public sealed class InternalServiceAuthMiddleware
{
    private readonly RequestDelegate _next;

    public InternalServiceAuthMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/internal"))
        {
            if (context.User.Identity?.IsAuthenticated != true)
            {
                var result = await context.AuthenticateAsync(VitalsAuthenticationExtensions.ServiceKeyScheme);
                if (result.Succeeded && result.Principal is not null)
                    context.User = result.Principal;
            }

            if (context.User.Identity?.IsAuthenticated != true)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Internal endpoints require service authentication." });
                return;
            }
        }

        await _next(context);
    }
}
