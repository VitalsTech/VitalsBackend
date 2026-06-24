using ConsultationService.Application.Interfaces;
using ConsultationService.Application.Options;
using ConsultationService.Application.Services;
using ConsultationService.Infrastructure.Clients;
using ConsultationService.Infrastructure.Data;
using ConsultationService.Infrastructure.Hosted;
using ConsultationService.Infrastructure.Messaging;
using ConsultationService.Infrastructure.Notifications;
using ConsultationService.Infrastructure.Repositories;
using ConsultationService.Infrastructure.State;
using ConsultationService.Infrastructure.Video;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitals.Messaging;
using Vitals.ESignature;

namespace ConsultationService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddConsultationInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtValidationOptions>(configuration.GetSection(JwtValidationOptions.SectionName));
        services.Configure<MedicalRecordServiceOptions>(configuration.GetSection(MedicalRecordServiceOptions.SectionName));
        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));
        services.Configure<ConsultationOptions>(configuration.GetSection(ConsultationOptions.SectionName));
        services.Configure<SfuOptions>(configuration.GetSection(SfuOptions.SectionName));
        services.AddVitalsESignature(configuration);
        services.AddVitalsKafka(configuration, options =>
        {
            var kafka = configuration.GetSection(KafkaOptions.SectionName).Get<KafkaOptions>() ?? new KafkaOptions();
            options.Enabled = kafka.Enabled;
            options.BootstrapServers = kafka.BootstrapServers;
            options.ConsumerGroupId = kafka.ConsumerGroupId;
            options.ClientId = kafka.ClientId;
        });

        services.AddDbContext<ConsultationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IConsultationRepository, ConsultationRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IConsultationService, ConsultationAppService>();
        services.AddSingleton<ISessionStateStore, InMemorySessionStateStore>();
        services.AddSingleton<IConsultationEventPublisher, KafkaConsultationEventPublisher>();
        services.AddSfuSignaling(configuration);
        services.AddScoped<IConsultationChatNotifier, SignalRConsultationChatNotifier>();
        services.AddHostedService<RoutingDecisionConsumerHostedService>();
        services.AddHostedService<SessionTimeoutHostedService>();

        services.AddHttpClient<IMedicalRecordEventClient, MedicalRecordEventClient>(client =>
        {
            var apiKey = configuration["ServiceAuth:ApiKey"];
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                client.DefaultRequestHeaders.Add("X-Service-Key", apiKey);
                client.DefaultRequestHeaders.Add("X-Service-Name", "consultation-service");
            }
        });
        services.AddSignalR();

        return services;
    }
}
