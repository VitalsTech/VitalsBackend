using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Vitals.Messaging;

public abstract class KafkaConsumerHostedService : BackgroundService
{
    private readonly ILogger _logger;
    private readonly KafkaConnectionOptions _options;

    protected KafkaConsumerHostedService(ILogger logger, IOptions<KafkaConnectionOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    protected abstract IReadOnlyList<string> Topics { get; }

    protected abstract Task HandleMessageAsync(string topic, string? key, string payload, CancellationToken cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Yield immediately so the synchronous, blocking consume loop below never
        // runs on the host startup thread (otherwise it blocks Kestrel from binding).
        await Task.Yield();

        if (!_options.Enabled)
        {
            _logger.LogInformation(
                "Kafka consumer disabled for {Service}. Topics={Topics}",
                GetType().Name,
                string.Join(", ", Topics));
            return;
        }

        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.ConsumerGroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            ClientId = _options.ClientId,
            AllowAutoCreateTopics = true
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(Topics);

        _logger.LogInformation(
            "Kafka consumer started. Group={Group} Topics={Topics}",
            _options.ConsumerGroupId,
            string.Join(", ", Topics));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);
                if (result.Message.Value is null)
                    continue;

                await HandleMessageAsync(result.Topic, result.Message.Key, result.Message.Value, stoppingToken)
                    .ConfigureAwait(false);

                consumer.Commit(result);
            }
            catch (ConsumeException ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Kafka consume error");
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Unexpected Kafka consumer error");
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken).ConfigureAwait(false);
            }
        }

        consumer.Close();
    }
}
