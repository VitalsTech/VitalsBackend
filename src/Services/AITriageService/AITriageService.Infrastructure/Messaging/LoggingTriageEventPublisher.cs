using AITriageService.Application.DTOs;
using AITriageService.Application.Interfaces;
using AITriageService.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AITriageService.Infrastructure.Messaging;

public sealed class LoggingTriageEventPublisher : ITriageEventPublisher
{
    private readonly ILogger<LoggingTriageEventPublisher> _logger;
    private readonly KafkaOptions _kafka;
    private readonly IRoutingDispatchClient _routing;

    public LoggingTriageEventPublisher(
        ILogger<LoggingTriageEventPublisher> logger,
        IOptions<KafkaOptions> kafka,
        IRoutingDispatchClient routing)
    {
        _logger = logger;
        _kafka = kafka.Value;
        _routing = routing;
    }

    public async Task<RoutingDecisionSummaryDto?> PublishTriageCompletedAsync(
        Guid sessionId,
        Guid patientId,
        LlmTriageResultDto result,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Publish triage.completed to {Topic}: session={SessionId} patient={PatientId} urgency={Urgency} enabled={Enabled}",
            _kafka.TriageCompletedTopic,
            sessionId,
            patientId,
            result.UrgencyLevel,
            _kafka.Enabled);

        if (_kafka.Enabled)
            return null;

        return await _routing.DispatchTriageCompletedAsync(sessionId, patientId, result, cancellationToken);
    }
}
