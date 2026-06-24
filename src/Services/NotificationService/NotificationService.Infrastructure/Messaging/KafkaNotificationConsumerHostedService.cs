using NotificationService.Application.Interfaces;
using NotificationService.Application.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NotificationService.Infrastructure.Messaging;

public sealed class KafkaNotificationConsumerHostedService : BackgroundService
{
    private readonly ILogger<KafkaNotificationConsumerHostedService> _logger;
    private readonly KafkaOptions _kafka;

    public KafkaNotificationConsumerHostedService(ILogger<KafkaNotificationConsumerHostedService> logger, IOptions<KafkaOptions> kafka)
    {
        _logger = logger;
        _kafka = kafka.Value;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_kafka.Enabled)
        {
            _logger.LogInformation(
                "Kafka consumer disabled. Process events via POST /internal/notifications/events. Topics={Topics}",
                string.Join(", ", _kafka.Topics));
            return Task.CompletedTask;
        }

        _logger.LogWarning("Kafka.Enabled=true but Confluent consumer is not implemented yet.");
        return Task.CompletedTask;
    }
}
