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
using Vitals.Messaging;

namespace AITriageService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTriageInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MedicalRecordServiceOptions>(configuration.GetSection(MedicalRecordServiceOptions.SectionName));
        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));
        services.Configure<MlServicesOptions>(configuration.GetSection(MlServicesOptions.SectionName));
        services.AddVitalsKafka(configuration, options =>
        {
            var kafka = configuration.GetSection(KafkaOptions.SectionName).Get<KafkaOptions>() ?? new KafkaOptions();
            options.Enabled = kafka.Enabled;
            options.BootstrapServers = kafka.BootstrapServers;
            options.ClientId = kafka.ClientId;
        });

        services.AddDbContext<TriageDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<ITriageSessionRepository, TriageSessionRepository>();
        services.AddSingleton<ISymptomParser, RuleBasedSymptomParser>();
        RegisterMlServices(services, configuration);
        services.AddScoped<ITriageOrchestrator, TriageOrchestrator>();
        services.AddSingleton<ITriageEventPublisher, KafkaTriageEventPublisher>();

        services.AddHttpClient<IMedicalRecordContextClient, MedicalRecordContextClient>();

        return services;
    }

    private static void RegisterMlServices(IServiceCollection services, IConfiguration configuration)
    {
        var ml = configuration.GetSection(MlServicesOptions.SectionName).Get<MlServicesOptions>() ?? new MlServicesOptions();
        if (ml.UseStubModels)
        {
            services.AddScoped<INerService, StubNerService>();
            services.AddScoped<ILlmTriageService, StubLlmTriageService>();
            return;
        }

        services.AddHttpClient<INerService, HttpNerService>(client =>
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(ml.TimeoutSeconds, 5, 120)));
        services.AddHttpClient<ILlmTriageService, HttpLlmTriageService>(client =>
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(ml.TimeoutSeconds, 5, 120)));
    }
}
