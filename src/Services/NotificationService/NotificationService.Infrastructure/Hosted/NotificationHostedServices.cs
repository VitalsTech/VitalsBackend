using NotificationService.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NotificationService.Infrastructure.Hosted;

public sealed class NotificationRetryHostedService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<NotificationRetryHostedService> _logger;

    public NotificationRetryHostedService(IServiceProvider services, ILogger<NotificationRetryHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
                var orchestrator = scope.ServiceProvider.GetRequiredService<INotificationOrchestrator>();
                var pending = await repo.GetPendingRetriesAsync(DateTime.UtcNow, stoppingToken);

                foreach (var delivery in pending)
                {
                    _logger.LogInformation("Retrying delivery {DeliveryId}", delivery.Id);
                    await orchestrator.RetryDeliveryAsync(delivery.Id, stoppingToken);
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Retry scan failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }
}

public sealed class ProcessedEventCleanupHostedService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ProcessedEventCleanupHostedService> _logger;

    public ProcessedEventCleanupHostedService(IServiceProvider services, ILogger<ProcessedEventCleanupHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
                await repo.CleanupOldProcessedEventsAsync(DateTime.UtcNow.AddDays(-7), stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Processed event cleanup failed");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
