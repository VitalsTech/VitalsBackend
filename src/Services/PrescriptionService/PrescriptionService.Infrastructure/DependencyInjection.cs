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
        services.Configure<ESignatureOptions>(configuration.GetSection(ESignatureOptions.SectionName));

        services.AddDbContext<PrescriptionDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
        services.AddScoped<IPrescriptionService, PrescriptionAppService>();
        services.AddSingleton<IPrescriptionValidationEngine, RuleBasedPrescriptionValidationEngine>();
        services.AddSingleton<IPrescriptionQrService, PrescriptionQrService>();
        services.AddSingleton<IPatientInstructionGenerator, TemplatePatientInstructionGenerator>();
        services.AddSingleton<IPrescriptionEventPublisher, LoggingPrescriptionEventPublisher>();
        services.AddSingleton<IUserPermissionClient, StubUserPermissionClient>();
        services.AddSingleton<IPharmacyIntegrationClient, StubPharmacyIntegrationClient>();
        services.AddSingleton<IESignatureService, StubESignatureService>();
        services.AddHostedService<PrescriptionExpiryHostedService>();

        services.AddHttpClient<IMedicalRecordContextClient, MedicalRecordContextClient>();
        services.AddHttpClient<IMedicalRecordEventClient, MedicalRecordEventClient>();

        return services;
    }
}
