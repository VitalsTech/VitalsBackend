using AnalyticsService.Application.DTOs;
using AnalyticsService.Application.Interfaces;
using AnalyticsService.Application.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vitals.Messaging;

namespace AnalyticsService.Infrastructure.Messaging;

public sealed class AnalyticsEventsConsumerHostedService : KafkaConsumerHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaOptions _kafka;

    public AnalyticsEventsConsumerHostedService(
        ILogger<AnalyticsEventsConsumerHostedService> logger,
        IOptions<KafkaConnectionOptions> kafkaConnection,
        IOptions<KafkaOptions> kafka,
        IServiceScopeFactory scopeFactory)
        : base(logger, kafkaConnection)
    {
        _scopeFactory = scopeFactory;
        _kafka = kafka.Value;
    }

    protected override IReadOnlyList<string> Topics =>
    [
        _kafka.ConsultationCompletedTopic,
        _kafka.PrescriptionIssuedTopic,
        _kafka.TriageCompletedTopic,
        _kafka.PaymentCompletedTopic
    ];

    protected override async Task HandleMessageAsync(
        string topic,
        string? key,
        string payload,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var analytics = scope.ServiceProvider.GetRequiredService<IAnalyticsService>();
        await analytics.RecordMetricAsync(new RecordMetricRequest
        {
            MetricName = topic,
            EventType = topic,
            Value = 1,
            PayloadJson = payload
        }, cancellationToken);
    }
}
