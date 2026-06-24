using System.Text.Json;
using RoutingService.Application.Interfaces;
using RoutingService.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vitals.Messaging;

namespace RoutingService.Infrastructure.Messaging;

public sealed class KafkaRoutingEventPublisher : IRoutingEventPublisher
{
    private readonly ILogger<KafkaRoutingEventPublisher> _logger;
    private readonly KafkaOptions _kafka;
    private readonly IKafkaMessageProducer _producer;

    public KafkaRoutingEventPublisher(
        ILogger<KafkaRoutingEventPublisher> logger,
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
            _logger.LogInformation("Kafka disabled; skip routing publish to {Topic}: {@Payload}", topic, payload);
            return;
        }

        var json = JsonSerializer.Serialize(payload);
        var key = TryGetPatientId(payload)?.ToString();
        await _producer.ProduceAsync(topic, key, json, cancellationToken).ConfigureAwait(false);
    }

    private static Guid? TryGetPatientId(object payload)
    {
        if (payload is null)
            return null;

        var json = JsonSerializer.Serialize(payload);
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("PatientId", out var patientId) &&
            patientId.TryGetGuid(out var guid))
        {
            return guid;
        }

        return null;
    }
}
