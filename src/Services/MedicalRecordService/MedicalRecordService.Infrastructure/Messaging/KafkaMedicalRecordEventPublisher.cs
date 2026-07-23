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
        string? payloadJson = null,
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

        object? payloadObj = null;
        if (!string.IsNullOrWhiteSpace(payloadJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(payloadJson);
                payloadObj = doc.RootElement.Clone();
            }
            catch (JsonException)
            {
                payloadObj = payloadJson;
            }
        }

        var envelope = new
        {
            PatientId = patientId,
            EventId = eventId,
            EventType = eventType,
            Version = version,
            OccurredAt = DateTime.UtcNow,
            Payload = payloadObj
        };

        await _producer.ProduceAsync(
                _kafka.MedicalEventsTopic,
                patientId.ToString(),
                JsonSerializer.Serialize(envelope),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public Task PublishPatientMoodUpdatedAsync(
        Guid patientId,
        Guid medicalEventId,
        IReadOnlyList<Guid> recipientDoctorIds,
        IReadOnlyDictionary<string, string> templateData,
        string priority,
        CancellationToken cancellationToken = default) =>
        PublishDoctorAlertAsync(
            _kafka.PatientMoodUpdatedTopic,
            "patient.mood.updated",
            patientId,
            medicalEventId,
            recipientDoctorIds,
            templateData,
            priority,
            cancellationToken);

    public Task PublishPatientTriageCompletedAsync(
        Guid patientId,
        Guid medicalEventId,
        IReadOnlyList<Guid> recipientDoctorIds,
        IReadOnlyDictionary<string, string> templateData,
        string priority,
        CancellationToken cancellationToken = default) =>
        PublishDoctorAlertAsync(
            _kafka.PatientTriageCompletedTopic,
            "patient.triage.completed",
            patientId,
            medicalEventId,
            recipientDoctorIds,
            templateData,
            priority,
            cancellationToken);

    private async Task PublishDoctorAlertAsync(
        string topic,
        string eventType,
        Guid patientId,
        Guid medicalEventId,
        IReadOnlyList<Guid> recipientDoctorIds,
        IReadOnlyDictionary<string, string> templateData,
        string priority,
        CancellationToken cancellationToken)
    {
        if (!_kafka.Enabled)
        {
            _logger.LogInformation(
                "Kafka disabled; skip {EventType} for patient {PatientId} doctors={Count}",
                eventType,
                patientId,
                recipientDoctorIds.Count);
            return;
        }

        if (recipientDoctorIds.Count == 0)
            return;

        var envelope = new
        {
            EventType = eventType,
            PatientId = patientId,
            MedicalEventId = medicalEventId,
            RecipientDoctorIds = recipientDoctorIds,
            Priority = priority,
            TemplateData = templateData,
            OccurredAt = DateTime.UtcNow
        };

        await _producer.ProduceAsync(
                topic,
                patientId.ToString(),
                JsonSerializer.Serialize(envelope),
                cancellationToken)
            .ConfigureAwait(false);
    }
}
