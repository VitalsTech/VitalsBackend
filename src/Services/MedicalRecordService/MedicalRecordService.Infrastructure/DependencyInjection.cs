using MedicalRecordService.Application.Interfaces;
using MedicalRecordService.Application.Options;
using MedicalRecordService.Application.Services;
using MedicalRecordService.Domain.Interfaces;
using MedicalRecordService.Infrastructure.Data;
using MedicalRecordService.Infrastructure.Messaging;
using MedicalRecordService.Infrastructure.Repositories;
using MedicalRecordService.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitals.Messaging;
using Vitals.ObjectStorage;

namespace MedicalRecordService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MedicalRecordOptions>(configuration.GetSection(MedicalRecordOptions.SectionName));
        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));
        services.AddVitalsKafka(configuration, options =>
        {
            var kafka = configuration.GetSection(KafkaOptions.SectionName).Get<KafkaOptions>() ?? new KafkaOptions();
            options.Enabled = kafka.Enabled;
            options.BootstrapServers = kafka.BootstrapServers;
            options.ClientId = "medical-record-service";
        });
        services.AddVitalsObjectStorage(configuration);

        services.AddDbContext<MedicalRecordDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IMedicalEventRepository, MedicalEventRepository>();
        services.AddScoped<IPatientSnapshotRepository, PatientSnapshotRepository>();
        services.AddScoped<IProjectionRepository, ProjectionRepository>();
        services.AddScoped<IAccessGrantRepository, AccessGrantRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IPatientAttachmentRepository, PatientAttachmentRepository>();

        services.AddScoped<ProjectionUpdater>();
        services.AddScoped<IAccessControlService, AccessControlService>();
        services.AddScoped<IMedicalEventService, MedicalEventService>();
        services.AddScoped<IPatientRecordQueryService, PatientRecordQueryService>();
        services.AddScoped<IAccessGrantService, AccessGrantService>();
        services.AddScoped<IPatientAttachmentService, PatientAttachmentService>();
        services.AddSingleton<IMedicalRecordEventPublisher, KafkaMedicalRecordEventPublisher>();

        return services;
    }
}
