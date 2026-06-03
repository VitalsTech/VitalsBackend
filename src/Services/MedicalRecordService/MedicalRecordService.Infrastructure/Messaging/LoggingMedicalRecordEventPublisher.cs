using MedicalRecordService.Application.Interfaces;
using MedicalRecordService.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MedicalRecordService.Infrastructure.Messaging;

/// <summary>
/// Kafka publisher placeholder: logs outbound events until Confluent producer is wired.
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
}
