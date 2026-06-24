using PrescriptionService.Application.Interfaces;
using PrescriptionService.Application.Options;
using PrescriptionService.Application.Services;
using PrescriptionService.Infrastructure.Clients;
using PrescriptionService.Infrastructure.Data;
using PrescriptionService.Infrastructure.Hosted;
using PrescriptionService.Infrastructure.Messaging;
using PrescriptionService.Infrastructure.Repositories;
using PrescriptionService.Infrastructure.Services;
using PrescriptionService.Infrastructure.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitals.Messaging;
using Vitals.ESignature;

namespace PrescriptionService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPrescriptionInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtValidationOptions>(configuration.GetSection(JwtValidationOptions.SectionName));
        services.Configure<MedicalRecordServiceOptions>(configuration.GetSection(MedicalRecordServiceOptions.SectionName));
        services.Configure<UserServiceOptions>(configuration.GetSection(UserServiceOptions.SectionName));
        services.Configure<IntegrationServiceOptions>(configuration.GetSection(IntegrationServiceOptions.SectionName));
        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));
        services.Configure<PrescriptionOptions>(configuration.GetSection(PrescriptionOptions.SectionName));
        services.AddVitalsESignature(configuration);
        services.AddVitalsKafka(configuration, options =>
        {
            var kafka = configuration.GetSection(KafkaOptions.SectionName).Get<KafkaOptions>() ?? new KafkaOptions();
            options.Enabled = kafka.Enabled;
            options.BootstrapServers = kafka.BootstrapServers;
            options.ClientId = kafka.ClientId;
        });

        services.AddDbContext<PrescriptionDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
        services.AddScoped<IPrescriptionService, PrescriptionAppService>();
        services.AddSingleton<IPrescriptionValidationEngine, RuleBasedPrescriptionValidationEngine>();
        services.AddSingleton<IPrescriptionQrService, PrescriptionQrService>();
        services.AddSingleton<IPatientInstructionGenerator, TemplatePatientInstructionGenerator>();
        services.AddSingleton<IPrescriptionEventPublisher, KafkaPrescriptionEventPublisher>();
        services.AddSingleton<IUserPermissionClient, StubUserPermissionClient>();
        services.AddSingleton<IPharmacyIntegrationClient, StubPharmacyIntegrationClient>();
        services.AddSingleton<IESignatureService, PrescriptionESignatureAdapter>();
        services.AddHostedService<PrescriptionExpiryHostedService>();

        services.AddHttpClient<IMedicalRecordContextClient, MedicalRecordContextClient>();
        services.AddHttpClient<IMedicalRecordEventClient, MedicalRecordEventClient>();
        services.AddHttpClient<IEgiszClient, IntegrationEgiszClient>((sp, client) =>
        {
            var integration = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<IntegrationServiceOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(integration.BaseUrl))
                client.BaseAddress = new Uri(integration.BaseUrl.TrimEnd('/') + "/");

            var apiKey = configuration["ServiceAuth:ApiKey"];
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                client.DefaultRequestHeaders.Add("X-Service-Key", apiKey);
                client.DefaultRequestHeaders.Add("X-Service-Name", "prescription-service");
            }
        });

        return services;
    }
}
