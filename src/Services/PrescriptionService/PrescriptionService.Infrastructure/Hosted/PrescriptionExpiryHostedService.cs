using PrescriptionService.Application.Interfaces;
using PrescriptionService.Application.Options;
using PrescriptionService.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PrescriptionService.Infrastructure.Hosted;

public sealed class PrescriptionExpiryHostedService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly PrescriptionOptions _options;
    private readonly KafkaOptions _kafka;
    private readonly ILogger<PrescriptionExpiryHostedService> _logger;

    public PrescriptionExpiryHostedService(
        IServiceProvider services,
        IOptions<PrescriptionOptions> options,
        IOptions<KafkaOptions> kafka,
        ILogger<PrescriptionExpiryHostedService> logger)
    {
        _services = services;
        _options = options.Value;
        _kafka = kafka.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            if (now.Hour == 8 && now.Minute < 2)
                await RunDailyTasksAsync(stoppingToken);

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task RunDailyTasksAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IPrescriptionRepository>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPrescriptionEventPublisher>();

        var expiringDate = DateTime.UtcNow.Date.AddDays(_options.ExpiringSoonDays);
        var expiring = await repo.GetExpiringSoonAsync(expiringDate, cancellationToken);
        foreach (var group in expiring.GroupBy(x => x.PatientId))
        {
            await publisher.PublishAsync(_kafka.PrescriptionExpiringSoonTopic, new
            {
                PatientId = group.Key,
                PrescriptionIds = group.Select(x => x.Id).ToList(),
                ExpiresOn = expiringDate
            }, cancellationToken);
        }

        var expired = await repo.GetExpiredCandidatesAsync(DateTime.UtcNow, cancellationToken);
        foreach (var prescription in expired)
        {
            prescription.Status = PrescriptionStatus.Expired;
            prescription.UpdatedAt = DateTime.UtcNow;
            await repo.SaveAsync(prescription, cancellationToken);
            await publisher.PublishAsync(_kafka.PrescriptionExpiredTopic, new { prescription.Id, prescription.PatientId }, cancellationToken);
            _logger.LogInformation("Prescription {Id} marked expired", prescription.Id);
        }
    }
}
