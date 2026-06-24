using AnalyticsService.Application.Interfaces;
using AnalyticsService.Application.Options;
using AnalyticsService.Application.Services;
using AnalyticsService.Infrastructure.Data;
using AnalyticsService.Infrastructure.Messaging;
using AnalyticsService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitals.Messaging;

namespace AnalyticsService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAnalyticsInfrastructure(this IServiceCollection services, IConfiguration configuration)
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

        services.AddDbContext<AnalyticsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();
        services.AddScoped<IAnalyticsService, AnalyticsAppService>();
        services.AddHostedService<AnalyticsEventsConsumerHostedService>();

        return services;
    }
}
