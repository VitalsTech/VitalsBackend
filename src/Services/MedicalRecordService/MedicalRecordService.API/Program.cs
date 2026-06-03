using System.Security.Claims;
using System.Security.Cryptography;
using MedicalRecordService.API.Middleware;
using MedicalRecordService.API.Validators;
using MedicalRecordService.Application.Options;
using MedicalRecordService.Infrastructure;
using MedicalRecordService.Infrastructure.Data;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace MedicalRecordService.API;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Medical Record Service", Version = "v1" });
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
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddFluentValidationAutoValidation();
        builder.Services.AddValidatorsFromAssemblyContaining<AppendEventRequestValidator>();

        ConfigureJwtAuthentication(builder);

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<MedicalRecordDbContext>().Database.Migrate();
        }

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

    private static void ConfigureJwtAuthentication(WebApplicationBuilder builder)
    {
        var jwtSection = builder.Configuration.GetSection(JwtValidationOptions.SectionName);
        var jwt = jwtSection.Get<JwtValidationOptions>() ?? new JwtValidationOptions();

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                if (!string.IsNullOrWhiteSpace(jwt.RsaPublicKeyPem))
                {
                    var rsa = RSA.Create();
                    rsa.ImportFromPem(jwt.RsaPublicKeyPem);
                    options.TokenValidationParameters = CreateParameters(jwt, new RsaSecurityKey(rsa));
                }
                else if (builder.Environment.IsDevelopment())
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

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (context.Request.Path.StartsWithSegments("/internal") &&
                            jwt.AllowDevelopmentHeaderFallback &&
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
