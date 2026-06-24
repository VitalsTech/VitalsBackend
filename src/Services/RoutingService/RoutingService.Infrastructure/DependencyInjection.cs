using RoutingService.Application.Interfaces;
using RoutingService.Application.Options;
using RoutingService.Application.Services;
using RoutingService.Infrastructure.Clients;
using RoutingService.Infrastructure.Data;
using RoutingService.Infrastructure.Messaging;
using RoutingService.Infrastructure.Repositories;
using RoutingService.Infrastructure.Routing;
using RoutingService.Infrastructure.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitals.Messaging;

namespace RoutingService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddRoutingInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtValidationOptions>(configuration.GetSection(JwtValidationOptions.SectionName));
        services.Configure<MedicalRecordServiceOptions>(configuration.GetSection(MedicalRecordServiceOptions.SectionName));
        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));
        services.Configure<RoutingEngineOptions>(configuration.GetSection(RoutingEngineOptions.SectionName));
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));
        services.AddVitalsKafka(configuration, options =>
        {
            var kafka = configuration.GetSection(KafkaOptions.SectionName).Get<KafkaOptions>() ?? new KafkaOptions();
            options.Enabled = kafka.Enabled;
            options.BootstrapServers = kafka.BootstrapServers;
            options.ConsumerGroupId = kafka.ConsumerGroupId;
            options.ClientId = kafka.ClientId;
        });

        services.AddDbContext<RoutingDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IRoutingDecisionRepository, RoutingDecisionRepository>();
        services.AddScoped<IPatientRouteRepository, PatientRouteRepository>();
        services.AddSingleton<IRoutingEngine, RuleBasedRoutingEngine>();
        services.AddDoctorScheduler(configuration);
        services.AddScoped<IRoutingOrchestrator, RoutingOrchestrator>();
        services.AddSingleton<IRoutingEventPublisher, KafkaRoutingEventPublisher>();
        services.AddHostedService<TriageCompletedConsumerHostedService>();

        services.AddHttpClient<IMedicalRecordContextClient, MedicalRecordContextClient>();

        return services;
    }
}
