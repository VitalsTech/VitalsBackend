using System.Security.Claims;
using System.Security.Cryptography;
using AITriageService.API.Middleware;
using AITriageService.API.Validators;
using AITriageService.Application.Options;
using AITriageService.Infrastructure;
using AITriageService.Infrastructure.Data;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace AITriageService.API;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "AI Triage Service", Version = "v1" });
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

        builder.Services.Configure<JwtValidationOptions>(builder.Configuration.GetSection(JwtValidationOptions.SectionName));
        builder.Services.AddTriageInfrastructure(builder.Configuration);
        builder.Services.AddFluentValidationAutoValidation();
        builder.Services.AddValidatorsFromAssemblyContaining<CreateTriageSessionRequestValidator>();

        ConfigureJwt(builder);

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
            scope.ServiceProvider.GetRequiredService<TriageDbContext>().Database.Migrate();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseMiddleware<GlobalExceptionHandler>();
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.Run();
    }

    private static void ConfigureJwt(WebApplicationBuilder builder)
    {
        var jwt = builder.Configuration.GetSection(JwtValidationOptions.SectionName).Get<JwtValidationOptions>()
            ?? new JwtValidationOptions();

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                if (!string.IsNullOrWhiteSpace(jwt.JwksUrl))
                {
                    try
                    {
                        using var client = new HttpClient();
                        var json = client.GetStringAsync(jwt.JwksUrl).GetAwaiter().GetResult();
                        var set = new JsonWebKeySet(json);
                        var key = set.GetSigningKeys().First();
                        options.TokenValidationParameters = CreateParameters(jwt, key);
                        return;
                    }
                    catch when (builder.Environment.IsDevelopment())
                    {
                        // fallback below
                    }
                }

                if (builder.Environment.IsDevelopment())
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
            });

        builder.Services.AddAuthorization();
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
        RoleClaimType = ClaimTypes.Role
    };
}
