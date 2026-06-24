using System.Text.Json;
using IntegrationService.Application.DTOs;
using IntegrationService.Application.Interfaces;
using IntegrationService.Application.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vitals.Messaging;

namespace IntegrationService.Infrastructure.Messaging;

public sealed class EmergencyRequiredConsumerHostedService : KafkaConsumerHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaOptions _kafka;

    public EmergencyRequiredConsumerHostedService(
        ILogger<EmergencyRequiredConsumerHostedService> logger,
        IOptions<KafkaConnectionOptions> kafkaConnection,
        IOptions<KafkaOptions> kafka,
        IServiceScopeFactory scopeFactory)
        : base(logger, kafkaConnection)
    {
        _scopeFactory = scopeFactory;
        _kafka = kafka.Value;
    }

    protected override IReadOnlyList<string> Topics => [_kafka.EmergencyRequiredTopic];

    protected override async Task HandleMessageAsync(string topic, string? key, string payload, CancellationToken cancellationToken)
    {
        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;

        var request = new EmergencyDispatchRequest
        {
            PatientId = root.GetProperty("PatientId").GetGuid(),
            SessionId = root.GetProperty("SessionId").GetGuid(),
            Symptoms = root.TryGetProperty("Symptoms", out var symptoms) ? symptoms.GetString() : null,
            UrgencyLevel = root.TryGetProperty("UrgencyLevel", out var urgency) ? urgency.GetInt32() : 5
        };

        using var scope = _scopeFactory.CreateScope();
        var dispatch = scope.ServiceProvider.GetRequiredService<IIntegrationDispatchService>();
        await dispatch.DispatchEmergencyAsync(request, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class LabOrderRequiredConsumerHostedService : KafkaConsumerHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaOptions _kafka;

    public LabOrderRequiredConsumerHostedService(
        ILogger<LabOrderRequiredConsumerHostedService> logger,
        IOptions<KafkaConnectionOptions> kafkaConnection,
        IOptions<KafkaOptions> kafka,
        IServiceScopeFactory scopeFactory)
        : base(logger, kafkaConnection)
    {
        _scopeFactory = scopeFactory;
        _kafka = kafka.Value;
    }

    protected override IReadOnlyList<string> Topics => [_kafka.LabOrderRequiredTopic];

    protected override async Task HandleMessageAsync(string topic, string? key, string payload, CancellationToken cancellationToken)
    {
        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;

        var labs = root.TryGetProperty("Labs", out var labsElement)
            ? labsElement.EnumerateArray().Select(x => x.GetString() ?? string.Empty).Where(x => x.Length > 0).ToList()
            : [];

        var request = new LabOrderRequest
        {
            PatientId = root.GetProperty("PatientId").GetGuid(),
            SessionId = root.GetProperty("SessionId").GetGuid(),
            Labs = labs,
            Priority = root.TryGetProperty("Priority", out var priority) ? priority.GetString() ?? "planned" : "planned"
        };

        using var scope = _scopeFactory.CreateScope();
        var dispatch = scope.ServiceProvider.GetRequiredService<IIntegrationDispatchService>();
        await dispatch.SendLabOrderAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
