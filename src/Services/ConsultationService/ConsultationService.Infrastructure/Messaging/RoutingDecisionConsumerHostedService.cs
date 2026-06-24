using System.Text.Json;
using ConsultationService.Application.DTOs;
using ConsultationService.Application.Interfaces;
using ConsultationService.Application.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vitals.Messaging;

namespace ConsultationService.Infrastructure.Messaging;

public sealed class RoutingDecisionConsumerHostedService : KafkaConsumerHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaOptions _kafka;

    public RoutingDecisionConsumerHostedService(
        ILogger<RoutingDecisionConsumerHostedService> logger,
        IOptions<KafkaConnectionOptions> kafkaConnection,
        IOptions<KafkaOptions> kafka,
        IServiceScopeFactory scopeFactory)
        : base(logger, kafkaConnection)
    {
        _scopeFactory = scopeFactory;
        _kafka = kafka.Value;
    }

    protected override IReadOnlyList<string> Topics => [_kafka.RoutingDecisionTopic];

    protected override async Task HandleMessageAsync(
        string topic,
        string? key,
        string payload,
        CancellationToken cancellationToken)
    {
        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;

        var outcomeType = root.TryGetProperty("OutcomeType", out var outcome) ? outcome.GetString() : null;
        if (outcomeType is not null &&
            !string.Equals(outcomeType, "Consultation", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(outcomeType, "LabsBeforeConsultation", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var decision = new RoutingDecisionEventDto
        {
            PatientId = root.GetProperty("PatientId").GetGuid(),
            SessionId = root.GetProperty("SessionId").GetGuid(),
            OutcomeType = outcomeType ?? "Consultation",
            Specialist = root.TryGetProperty("Specialist", out var specialist) ? specialist.GetString() : null,
            ConsultationFormat = root.TryGetProperty("ConsultationFormat", out var format) ? format.GetString() : null,
            DoctorId = root.TryGetProperty("DoctorId", out var doctorId) && doctorId.ValueKind != JsonValueKind.Null
                ? doctorId.GetGuid()
                : null,
            DoctorName = root.TryGetProperty("DoctorName", out var doctorName) ? doctorName.GetString() : null,
            Priority = root.TryGetProperty("Priority", out var priority) ? priority.GetInt32() : 0,
            EffectiveUrgencyLevel = root.TryGetProperty("EffectiveUrgencyLevel", out var urgency) ? urgency.GetInt32() : 3,
            PatientMessage = root.TryGetProperty("PatientMessage", out var message) ? message.GetString() : null
        };

        if (!decision.DoctorId.HasValue)
            return;

        using var scope = _scopeFactory.CreateScope();
        var consultations = scope.ServiceProvider.GetRequiredService<IConsultationService>();
        await consultations.CreateFromRoutingDecisionAsync(decision, cancellationToken).ConfigureAwait(false);
    }
}
