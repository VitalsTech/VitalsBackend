using System.Text.Json;
using MedicalRecordService.Application.Interfaces;
using MedicalRecordService.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vitals.Messaging;

namespace MedicalRecordService.Infrastructure.Messaging;

public sealed class KafkaMedicalRecordEventPublisher : IMedicalRecordEventPublisher
{
    private readonly ILogger<KafkaMedicalRecordEventPublisher> _logger;
    private readonly KafkaOptions _kafka;
    private readonly IKafkaMessageProducer _producer;

    public KafkaMedicalRecordEventPublisher(
        ILogger<KafkaMedicalRecordEventPublisher> logger,
        IOptions<KafkaOptions> kafka,
        IKafkaMessageProducer producer)
    {
        _logger = logger;
        _kafka = kafka.Value;
        _producer = producer;
    }

    public async Task PublishEventAppendedAsync(
        Guid patientId,
        Guid eventId,
        string eventType,
        long version,
        CancellationToken cancellationToken = default)
    {
        if (!_kafka.Enabled)
        {
            _logger.LogInformation(
                "Kafka disabled; medical event not published for patient {PatientId} event {EventId}",
                patientId,
                eventId);
            return;
        }

        var payload = new
        {
            PatientId = patientId,
            EventId = eventId,
            EventType = eventType,
            Version = version,
            OccurredAt = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(payload);
        await _producer.ProduceAsync(_kafka.MedicalEventsTopic, patientId.ToString(), json, cancellationToken)
            .ConfigureAwait(false);
    }
}
