using System.Text.Json;
using ConsultationService.Application.Interfaces;
using ConsultationService.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vitals.Messaging;

namespace ConsultationService.Infrastructure.Messaging;

public sealed class KafkaConsultationEventPublisher : IConsultationEventPublisher
{
    private readonly ILogger<KafkaConsultationEventPublisher> _logger;
    private readonly KafkaOptions _kafka;
    private readonly IKafkaMessageProducer _producer;

    public KafkaConsultationEventPublisher(
        ILogger<KafkaConsultationEventPublisher> logger,
        IOptions<KafkaOptions> kafka,
        IKafkaMessageProducer producer)
    {
        _logger = logger;
        _kafka = kafka.Value;
        _producer = producer;
    }

    public async Task PublishAsync(string topic, object payload, CancellationToken cancellationToken = default)
    {
        if (!_kafka.Enabled)
        {
            _logger.LogInformation("Kafka disabled; skip consultation publish to {Topic}: {@Payload}", topic, payload);
            return;
        }

        var json = JsonSerializer.Serialize(payload);
        await _producer.ProduceAsync(topic, null, json, cancellationToken).ConfigureAwait(false);
    }
}
