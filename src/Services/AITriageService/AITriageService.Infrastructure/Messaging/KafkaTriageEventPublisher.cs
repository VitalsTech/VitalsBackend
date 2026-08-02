using System.Text.Json;
using AITriageService.Application.DTOs;
using AITriageService.Application.Interfaces;
using AITriageService.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vitals.Messaging;

namespace AITriageService.Infrastructure.Messaging;

public sealed class KafkaTriageEventPublisher : ITriageEventPublisher
{
    private readonly ILogger<KafkaTriageEventPublisher> _logger;
    private readonly KafkaOptions _kafka;
    private readonly IKafkaMessageProducer _producer;
    private readonly IRoutingDispatchClient _routing;

    public KafkaTriageEventPublisher(
        ILogger<KafkaTriageEventPublisher> logger,
        IOptions<KafkaOptions> kafka,
        IKafkaMessageProducer producer,
        IRoutingDispatchClient routing)
    {
        _logger = logger;
        _kafka = kafka.Value;
        _producer = producer;
        _routing = routing;
    }

    public async Task<RoutingDecisionSummaryDto?> PublishTriageCompletedAsync(
        Guid sessionId,
        Guid patientId,
        LlmTriageResultDto result,
        CancellationToken cancellationToken = default)
    {
        if (!_kafka.Enabled)
        {
            _logger.LogInformation(
                "Kafka disabled; dispatching triage.completed via HTTP for session {SessionId}",
                sessionId);
            return await _routing.DispatchTriageCompletedAsync(sessionId, patientId, result, cancellationToken);
        }

        var payload = new
        {
            SessionId = sessionId,
            PatientId = patientId,
            result.UrgencyLevel,
            result.EmergencyWarning,
            PatientMessageSummary = result.RecommendedAction,
            Hypotheses = result.Hypotheses.Select(h => new { h.Condition, h.Probability }).ToList(),
            ExtractedSymptoms = result.AdditionalDataNeeded,
            result.RecommendedAction
        };

        var json = JsonSerializer.Serialize(payload);
        await _producer.ProduceAsync(_kafka.TriageCompletedTopic, patientId.ToString(), json, cancellationToken)
            .ConfigureAwait(false);
        return null;
    }
}
