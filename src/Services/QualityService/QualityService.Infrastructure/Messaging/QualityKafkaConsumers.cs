using System.Text.Json;
using QualityService.Application.Interfaces;
using QualityService.Application.Options;
using QualityService.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vitals.Messaging;

namespace QualityService.Infrastructure.Messaging;

public sealed class ConsultationQualityConsumerHostedService : KafkaConsumerHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaOptions _kafka;

    public ConsultationQualityConsumerHostedService(
        ILogger<ConsultationQualityConsumerHostedService> logger,
        IOptions<KafkaConnectionOptions> kafkaConnection,
        IOptions<KafkaOptions> kafka,
        IServiceScopeFactory scopeFactory)
        : base(logger, kafkaConnection)
    {
        _scopeFactory = scopeFactory;
        _kafka = kafka.Value;
    }

    protected override IReadOnlyList<string> Topics => [_kafka.ConsultationCompletedTopic];

    protected override async Task HandleMessageAsync(
        string topic,
        string? key,
        string payload,
        CancellationToken cancellationToken)
    {
        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;
        var consultationId = root.TryGetProperty("ConsultationId", out var idProp) && idProp.TryGetGuid(out var id)
            ? id
            : root.TryGetProperty("consultationId", out var camelId) && camelId.TryGetGuid(out var camelGuid)
                ? camelGuid
                : Guid.NewGuid();

        var doctorId = root.TryGetProperty("DoctorId", out var doctorProp) && doctorProp.TryGetGuid(out var doctorGuid)
            ? doctorGuid
            : root.TryGetProperty("doctorId", out var doctorCamel) && doctorCamel.TryGetGuid(out var doctorCamelGuid)
                ? doctorCamelGuid
                : (Guid?)null;

        var score = root.TryGetProperty("QualityScore", out var scoreProp) && scoreProp.TryGetDouble(out var explicitScore)
            ? explicitScore
            : 4.5;

        using var scope = _scopeFactory.CreateScope();
        var quality = scope.ServiceProvider.GetRequiredService<IQualityService>();
        await quality.RecordScoreAsync(new QualityScore
        {
            Id = Guid.NewGuid(),
            ConsultationId = consultationId,
            DoctorId = doctorId,
            Score = score,
            SourceEventType = topic,
            RecordedAt = DateTime.UtcNow
        }, cancellationToken);
    }
}
