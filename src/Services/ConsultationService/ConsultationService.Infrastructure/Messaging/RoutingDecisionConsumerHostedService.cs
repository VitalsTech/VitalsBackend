using ConsultationService.Application.Interfaces;
using ConsultationService.Application.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ConsultationService.Infrastructure.Messaging;

public sealed class RoutingDecisionConsumerHostedService : BackgroundService
{
    private readonly ILogger<RoutingDecisionConsumerHostedService> _logger;
    private readonly KafkaOptions _kafka;

    public RoutingDecisionConsumerHostedService(ILogger<RoutingDecisionConsumerHostedService> logger, IOptions<KafkaOptions> kafka)
    {
        _logger = logger;
        _kafka = kafka.Value;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_kafka.Enabled)
        {
            _logger.LogInformation(
                "Kafka consumer disabled. Create sessions via POST /internal/consultations/routing-decision. Topic={Topic}",
                _kafka.RoutingDecisionTopic);
            return Task.CompletedTask;
        }

        _logger.LogWarning("Kafka.Enabled=true but Confluent consumer is not implemented yet. Topic={Topic}", _kafka.RoutingDecisionTopic);
        return Task.CompletedTask;
    }
}
