using ConsultationService.API.Middleware;
using ConsultationService.API.Validators;
using ConsultationService.Infrastructure;
using ConsultationService.Infrastructure.Data;
using ConsultationService.Infrastructure.Hubs;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Vitals.AspNetCore.Authentication;

namespace ConsultationService.API;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Consultation Service", Version = "v1" });
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

        builder.Services.AddConsultationInfrastructure(builder.Configuration);
        builder.Services.AddFluentValidationAutoValidation();
        builder.Services.AddValidatorsFromAssemblyContaining<CreateConsultationRequestValidator>();
        builder.Services.AddVitalsAuthentication(builder.Configuration, builder.Environment);
        builder.Services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            var prior = options.Events?.OnMessageReceived;
            options.Events ??= new JwtBearerEvents();
            options.Events.OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/consultation"))
                    context.Token = accessToken;

                return prior?.Invoke(context) ?? Task.CompletedTask;
            };
        });

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
            scope.ServiceProvider.GetRequiredService<ConsultationDbContext>().Database.Migrate();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseMiddleware<GlobalExceptionHandler>();
        app.UseWhen(
            context => !context.Request.Path.StartsWithSegments("/hubs"),
            branch => branch.UseHttpsRedirection());
        app.UseAuthentication();
        app.UseVitalsInternalServiceAuth();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHub<ConsultationHub>("/hubs/consultation");
        await app.RunAsync();
    }
}
