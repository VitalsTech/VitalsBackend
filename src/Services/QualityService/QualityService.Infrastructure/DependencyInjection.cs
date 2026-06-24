using QualityService.Application.Interfaces;
using QualityService.Application.Options;
using QualityService.Application.Services;
using QualityService.Infrastructure.Data;
using QualityService.Infrastructure.Messaging;
using QualityService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitals.Messaging;

namespace QualityService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddQualityInfrastructure(this IServiceCollection services, IConfiguration configuration)
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

        services.AddDbContext<QualityDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IQualityRepository, QualityRepository>();
        services.AddScoped<IQualityService, QualityAppService>();
        services.AddHostedService<ConsultationQualityConsumerHostedService>();

        return services;
    }
}
