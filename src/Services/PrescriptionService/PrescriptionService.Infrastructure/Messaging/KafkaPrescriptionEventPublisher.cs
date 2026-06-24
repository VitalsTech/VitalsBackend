using System.Text.Json;
using PrescriptionService.Application.Interfaces;
using PrescriptionService.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vitals.Messaging;

namespace PrescriptionService.Infrastructure.Messaging;

public sealed class KafkaPrescriptionEventPublisher : IPrescriptionEventPublisher
{
    private readonly ILogger<KafkaPrescriptionEventPublisher> _logger;
    private readonly KafkaOptions _kafka;
    private readonly IKafkaMessageProducer _producer;

    public KafkaPrescriptionEventPublisher(
        ILogger<KafkaPrescriptionEventPublisher> logger,
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
            _logger.LogInformation("Kafka disabled; prescription event not published to {Topic}", topic);
            return;
        }

        var key = ExtractKey(payload);
        var json = JsonSerializer.Serialize(payload);
        await _producer.ProduceAsync(topic, key, json, cancellationToken).ConfigureAwait(false);
    }

    private static string ExtractKey(object payload)
    {
        var type = payload.GetType();
        var prescriptionId = type.GetProperty("PrescriptionId")?.GetValue(payload);
        if (prescriptionId is Guid id)
            return id.ToString();

        var patientId = type.GetProperty("PatientId")?.GetValue(payload);
        if (patientId is Guid patient)
            return patient.ToString();

        return Guid.NewGuid().ToString();
    }
}
