using IntegrationService.Application.Interfaces;
using IntegrationService.Application.Options;
using IntegrationService.Application.Services;
using IntegrationService.Infrastructure.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitals.Messaging;
using Vitals.ObjectStorage;

namespace IntegrationService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIntegrationInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));
        services.AddVitalsKafka(configuration, options =>
        {
            var kafka = configuration.GetSection(KafkaOptions.SectionName).Get<KafkaOptions>() ?? new KafkaOptions();
            options.Enabled = kafka.Enabled;
            options.BootstrapServers = kafka.BootstrapServers;
            options.ConsumerGroupId = kafka.ConsumerGroupId;
            options.ClientId = kafka.ClientId;
        });

        services.AddVitalsObjectStorage(configuration);
        services.AddScoped<IIntegrationDispatchService, IntegrationDispatchService>();
        services.AddHostedService<EmergencyRequiredConsumerHostedService>();
        services.AddHostedService<LabOrderRequiredConsumerHostedService>();
        return services;
    }
}
