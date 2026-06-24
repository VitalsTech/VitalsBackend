using NotificationService.Application.Interfaces;
using NotificationService.Application.Options;
using NotificationService.Application.Services;
using NotificationService.Infrastructure.Channels;
using NotificationService.Infrastructure.Clients;
using NotificationService.Infrastructure.Data;
using NotificationService.Infrastructure.Hosted;
using NotificationService.Infrastructure.Messaging;
using NotificationService.Infrastructure.Preferences;
using NotificationService.Infrastructure.Repositories;
using NotificationService.Infrastructure.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitals.Messaging;

namespace NotificationService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtValidationOptions>(configuration.GetSection(JwtValidationOptions.SectionName));
        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));
        services.Configure<UserServiceOptions>(configuration.GetSection(UserServiceOptions.SectionName));
        services.Configure<IntegrationServiceOptions>(configuration.GetSection(IntegrationServiceOptions.SectionName));
        services.Configure<NotificationOptions>(configuration.GetSection(NotificationOptions.SectionName));
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));
        services.AddVitalsKafka(configuration, options =>
        {
            var kafka = configuration.GetSection(KafkaOptions.SectionName).Get<KafkaOptions>() ?? new KafkaOptions();
            options.Enabled = kafka.Enabled;
            options.BootstrapServers = kafka.BootstrapServers;
            options.ConsumerGroupId = kafka.ConsumerGroupId;
            options.ClientId = kafka.ClientId;
        });

        services.AddDbContext<NotificationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationOrchestrator, NotificationOrchestrator>();
        services.AddScoped<INotificationService, NotificationAppService>();
        services.AddSingleton<ITemplateRenderer, TemplateRenderer>();
        services.AddSingleton<IEventChannelRouter, EventChannelRouter>();
        services.AddScoped<IUserPreferenceService, UserPreferenceService>();
        services.AddIntegrationChannelClient(configuration);

        services.AddScoped<IChannelDispatcher, PushChannelDispatcher>();
        services.AddScoped<IChannelDispatcher, SmsChannelDispatcher>();
        services.AddScoped<IChannelDispatcher, EmailChannelDispatcher>();
        services.AddScoped<IChannelDispatcher, VoiceChannelDispatcher>();

        services.AddHostedService<KafkaNotificationConsumerHostedService>();
        services.AddHostedService<NotificationRetryHostedService>();
        services.AddHostedService<ProcessedEventCleanupHostedService>();

        return services;
    }
}
