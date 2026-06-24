using RoutingService.Application.Interfaces;
using RoutingService.Application.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RoutingService.Infrastructure.Messaging;

public sealed class TriageCompletedConsumerHostedService : BackgroundService
{
    private readonly ILogger<TriageCompletedConsumerHostedService> _logger;
    private readonly KafkaOptions _kafka;

    public TriageCompletedConsumerHostedService(
        ILogger<TriageCompletedConsumerHostedService> logger,
        IOptions<KafkaOptions> kafka)
    {
        _logger = logger;
        _kafka = kafka.Value;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_kafka.Enabled)
        {
            _logger.LogInformation(
                "Kafka consumer disabled. Process triage via POST /internal/routing/triage-completed. Topic={Topic}",
                _kafka.TriageCompletedTopic);
            return Task.CompletedTask;
        }

        _logger.LogWarning(
            "Kafka.Enabled=true but Confluent consumer is not implemented yet. Topic={Topic}, group={Group}",
            _kafka.TriageCompletedTopic,
            _kafka.ConsumerGroupId);

        return Task.CompletedTask;
    }
}
