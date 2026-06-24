using ConsultationService.Application.Interfaces;
using ConsultationService.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ConsultationService.Infrastructure.Messaging;

public sealed class LoggingConsultationEventPublisher : IConsultationEventPublisher
{
    private readonly ILogger<LoggingConsultationEventPublisher> _logger;
    private readonly KafkaOptions _kafka;

    public LoggingConsultationEventPublisher(ILogger<LoggingConsultationEventPublisher> logger, IOptions<KafkaOptions> kafka)
    {
        _logger = logger;
        _kafka = kafka.Value;
    }

    public Task PublishAsync(string topic, object payload, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Publish consultation event to {Topic} (enabled={Enabled}): {@Payload}", topic, _kafka.Enabled, payload);
        return Task.CompletedTask;
    }
}
