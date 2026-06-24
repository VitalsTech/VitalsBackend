using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Vitals.Messaging;

public sealed class KafkaMessageProducer : IKafkaMessageProducer, IDisposable
{
    private readonly ILogger<KafkaMessageProducer> _logger;
    private readonly KafkaConnectionOptions _options;
    private readonly IProducer<string, string>? _producer;

    public KafkaMessageProducer(ILogger<KafkaMessageProducer> logger, IOptions<KafkaConnectionOptions> options)
    {
        _logger = logger;
        _options = options.Value;

        if (!_options.Enabled)
            return;

        var config = new ProducerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            ClientId = _options.ClientId,
            Acks = Acks.All,
            EnableIdempotence = true,
            MessageSendMaxRetries = 3
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task ProduceAsync(string topic, string? key, string payload, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || _producer is null)
        {
            _logger.LogDebug("Kafka disabled; skip publish to {Topic}", topic);
            return;
        }

        try
        {
            var result = await _producer.ProduceAsync(
                topic,
                new Message<string, string> { Key = key, Value = payload },
                cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Kafka published to {Topic} partition={Partition} offset={Offset}",
                topic,
                result.Partition.Value,
                result.Offset.Value);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Kafka publish failed for topic {Topic}", topic);
            throw;
        }
    }

    public void Dispose()
    {
        _producer?.Flush(TimeSpan.FromSeconds(5));
        _producer?.Dispose();
    }
}
