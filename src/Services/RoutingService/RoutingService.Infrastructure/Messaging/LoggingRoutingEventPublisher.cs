using RoutingService.Application.Interfaces;
using RoutingService.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RoutingService.Infrastructure.Messaging;

public sealed class LoggingRoutingEventPublisher : IRoutingEventPublisher
{
    private readonly ILogger<LoggingRoutingEventPublisher> _logger;
    private readonly KafkaOptions _kafka;

    public LoggingRoutingEventPublisher(ILogger<LoggingRoutingEventPublisher> logger, IOptions<KafkaOptions> kafka)
    {
        _logger = logger;
        _kafka = kafka.Value;
    }

    public Task PublishAsync(string topic, object payload, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Publish routing event to {Topic} (kafka enabled={Enabled}): {@Payload}",
            topic,
            _kafka.Enabled,
            payload);

        return Task.CompletedTask;
    }
}
