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

        services.AddDbContext<ConsultationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IConsultationRepository, ConsultationRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IConsultationService, ConsultationAppService>();
        services.AddSingleton<ISessionStateStore, InMemorySessionStateStore>();
        services.AddSingleton<IConsultationEventPublisher, LoggingConsultationEventPublisher>();
        services.AddSingleton<ISfuSignalingService, StubSfuSignalingService>();
        services.AddScoped<IConsultationChatNotifier, SignalRConsultationChatNotifier>();
        services.AddHostedService<RoutingDecisionConsumerHostedService>();
        services.AddHostedService<SessionTimeoutHostedService>();

        services.AddHttpClient<IMedicalRecordEventClient, MedicalRecordEventClient>();
        services.AddSignalR();

        return services;
    }
}
