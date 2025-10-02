using Confluent.Kafka;

namespace Kafka.Consumer.Services;

public interface IConsumerService
{
    Task ProcessMessageAsync(ConsumeResult<string, string> message, CancellationToken cancellationToken = default);
}