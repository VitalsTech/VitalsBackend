using System.IO.Compression;
using System.Security.Claims;
using ApiGateway.API.Middleware;
using ApiGateway.API.Validators;
using ApiGateway.Application.Options;
using ApiGateway.Infrastructure;
using ApiGateway.Infrastructure.Security;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Yarp.ReverseProxy.Transforms;

namespace ApiGateway.API;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Vitals API Gateway", Version = "v1" });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });
        });

        builder.Services.AddGatewayInfrastructure(builder.Configuration);
        builder.Services.AddFluentValidationAutoValidation();
        builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestDtoValidator>();
        builder.Services.AddValidatorsFromAssemblyContaining<UserSearchRequestDtoValidator>();

        ConfigureJwt(builder);
        ConfigureCompression(builder);
        ConfigureReverseProxy(builder);

        var app = builder.Build();

        if (!app.Environment.IsProduction())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            app.MapGet("/", () => Results.Redirect("/swagger"));
        }

        app.UseMiddleware<GlobalExceptionMiddleware>();
        app.UseResponseCompression();
        app.UseMiddleware<RequestIdMiddleware>();
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseMiddleware<JwtAuthenticationMiddleware>();
        app.UseMiddleware<RateLimitingMiddleware>();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "api-gateway" }));
        app.MapControllers();
        app.MapReverseProxy();

        app.Run();
    }

    private static void ConfigureJwt(WebApplicationBuilder builder)
    {
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();
        builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<JwtSigningKeyProvider, Microsoft.Extensions.Options.IOptions<JwtOptions>>((options, keys, jwt) =>
            {
                var jwtOptions = jwt.Value;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = keys.SigningKey,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "sub",
                    RoleClaimType = ClaimTypes.Role
                };
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return context.Response.WriteAsJsonAsync(new { error = "Unauthorized" });
                    }
                };
            });
        builder.Services.AddAuthorization();
    }

    private static void ConfigureCompression(WebApplicationBuilder builder)
    {
        builder.Services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/json"]);
        });
        builder.Services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
        builder.Services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
    }

    private static void ConfigureReverseProxy(WebApplicationBuilder builder)
    {
        builder.Services
            .AddReverseProxy()
            .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
            .AddTransforms(context =>
            {
                context.AddRequestTransform(transformContext =>
                {
                    transformContext.ProxyRequest.Headers.Remove("X-Request-ID");
                    transformContext.ProxyRequest.Headers.TryAddWithoutValidation(
                        "X-Request-ID",
                        transformContext.HttpContext.TraceIdentifier);
                    return ValueTask.CompletedTask;
                });
            });
    }
}
