using System.Text.Json;
using PaymentService.Application.Interfaces;
using PaymentService.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vitals.Messaging;

namespace PaymentService.Infrastructure.Messaging;

public sealed class KafkaPaymentEventPublisher : IPaymentEventPublisher
{
    private readonly ILogger<KafkaPaymentEventPublisher> _logger;
    private readonly KafkaOptions _kafka;
    private readonly IKafkaMessageProducer _producer;

    public KafkaPaymentEventPublisher(
        ILogger<KafkaPaymentEventPublisher> logger,
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
            _logger.LogInformation("Kafka disabled; payment event not published to {Topic}", topic);
            return;
        }

        var key = payload.GetType().GetProperty("Id")?.GetValue(payload)?.ToString() ?? Guid.NewGuid().ToString();
        await _producer.ProduceAsync(topic, key, JsonSerializer.Serialize(payload), cancellationToken).ConfigureAwait(false);
    }
}
