using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RoutingService.Application.DTOs;
using RoutingService.Application.Interfaces;
using RoutingService.Application.Options;
using Vitals.Messaging;

namespace RoutingService.Infrastructure.Messaging;

public sealed class TriageCompletedConsumerHostedService : KafkaConsumerHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaOptions _kafka;

    public TriageCompletedConsumerHostedService(
        ILogger<TriageCompletedConsumerHostedService> logger,
        IOptions<KafkaConnectionOptions> kafkaConnection,
        IOptions<KafkaOptions> kafka,
        IServiceScopeFactory scopeFactory)
        : base(logger, kafkaConnection)
    {
        _scopeFactory = scopeFactory;
        _kafka = kafka.Value;
    }

    protected override IReadOnlyList<string> Topics => [_kafka.TriageCompletedTopic];

    protected override async Task HandleMessageAsync(
        string topic,
        string? key,
        string payload,
        CancellationToken cancellationToken)
    {
        var triageEvent = JsonSerializer.Deserialize<TriageCompletedEventDto>(payload, JsonSerializerOptions)
            ?? throw new InvalidOperationException("Invalid triage.completed payload.");

        using var scope = _scopeFactory.CreateScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<IRoutingOrchestrator>();
        await orchestrator.ProcessTriageCompletedAsync(triageEvent, cancellationToken).ConfigureAwait(false);
    }

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
