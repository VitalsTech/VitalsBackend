using PrescriptionService.Application.Interfaces;
using PrescriptionService.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PrescriptionService.Infrastructure.Messaging;

public sealed class LoggingPrescriptionEventPublisher : IPrescriptionEventPublisher
{
    private readonly ILogger<LoggingPrescriptionEventPublisher> _logger;
    private readonly KafkaOptions _kafka;

    public LoggingPrescriptionEventPublisher(ILogger<LoggingPrescriptionEventPublisher> logger, IOptions<KafkaOptions> kafka)
    {
        _logger = logger;
        _kafka = kafka.Value;
    }

    public Task PublishAsync(string topic, object payload, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Publish prescription event to {Topic} (enabled={Enabled}): {@Payload}", topic, _kafka.Enabled, payload);
        return Task.CompletedTask;
    }
}
