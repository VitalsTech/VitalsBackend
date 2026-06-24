using AITriageService.Application.Interfaces;
using AITriageService.Application.Options;
using AITriageService.Application.Services;
using AITriageService.Domain.Interfaces;
using AITriageService.Infrastructure.Clients;
using AITriageService.Infrastructure.Data;
using AITriageService.Infrastructure.Messaging;
using AITriageService.Infrastructure.Ml;
using AITriageService.Infrastructure.Parsing;
using AITriageService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AITriageService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTriageInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MedicalRecordServiceOptions>(configuration.GetSection(MedicalRecordServiceOptions.SectionName));
        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));
        services.Configure<MlServicesOptions>(configuration.GetSection(MlServicesOptions.SectionName));

        services.AddDbContext<TriageDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<ITriageSessionRepository, TriageSessionRepository>();
        services.AddSingleton<ISymptomParser, RuleBasedSymptomParser>();
        services.AddScoped<INerService, StubNerService>();
        services.AddScoped<ILlmTriageService, StubLlmTriageService>();
        services.AddScoped<ITriageOrchestrator, TriageOrchestrator>();
        services.AddSingleton<ITriageEventPublisher, LoggingTriageEventPublisher>();

        services.AddHttpClient<IMedicalRecordContextClient, MedicalRecordContextClient>();

        return services;
    }
}
