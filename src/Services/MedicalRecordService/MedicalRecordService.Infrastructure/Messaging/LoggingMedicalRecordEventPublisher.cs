using MedicalRecordService.Application.Interfaces;
using MedicalRecordService.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MedicalRecordService.Infrastructure.Messaging;

/// <summary>
/// Kafka publisher placeholder: logs outbound events when Kafka producer is not used.
/// </summary>
public sealed class LoggingMedicalRecordEventPublisher : IMedicalRecordEventPublisher
{
    private readonly ILogger<LoggingMedicalRecordEventPublisher> _logger;
    private readonly KafkaOptions _kafka;

    public LoggingMedicalRecordEventPublisher(
        ILogger<LoggingMedicalRecordEventPublisher> logger,
        IOptions<KafkaOptions> kafka)
    {
        _logger = logger;
        _kafka = kafka.Value;
    }

    public Task PublishEventAppendedAsync(
        Guid patientId,
        Guid eventId,
        string eventType,
        long version,
        string? payloadJson = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Publish medical event to {Topic}: patient={PatientId} event={EventId} type={EventType} version={Version} enabled={Enabled}",
            _kafka.MedicalEventsTopic,
            patientId,
            eventId,
            eventType,
            version,
            _kafka.Enabled);

        return Task.CompletedTask;
    }

    public Task PublishPatientMoodUpdatedAsync(
        Guid patientId,
        Guid medicalEventId,
        IReadOnlyList<Guid> recipientDoctorIds,
        IReadOnlyDictionary<string, string> templateData,
        string priority,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Publish {Topic}: patient={PatientId} event={EventId} doctors={DoctorCount} priority={Priority} enabled={Enabled}",
            _kafka.PatientMoodUpdatedTopic,
            patientId,
            medicalEventId,
            recipientDoctorIds.Count,
            priority,
            _kafka.Enabled);

        return Task.CompletedTask;
    }

    public Task PublishPatientTriageCompletedAsync(
        Guid patientId,
        Guid medicalEventId,
        IReadOnlyList<Guid> recipientDoctorIds,
        IReadOnlyDictionary<string, string> templateData,
        string priority,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Publish {Topic}: patient={PatientId} event={EventId} doctors={DoctorCount} priority={Priority} enabled={Enabled}",
            _kafka.PatientTriageCompletedTopic,
            patientId,
            medicalEventId,
            recipientDoctorIds.Count,
            priority,
            _kafka.Enabled);

        return Task.CompletedTask;
    }
}
