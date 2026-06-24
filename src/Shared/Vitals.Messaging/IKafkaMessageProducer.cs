namespace Vitals.Messaging;

public interface IKafkaMessageProducer
{
    Task ProduceAsync(string topic, string? key, string payload, CancellationToken cancellationToken = default);
}
