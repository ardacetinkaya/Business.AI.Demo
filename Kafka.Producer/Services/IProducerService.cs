namespace Kafka.Producer.Services;

public interface IProducerService
{
    Task PublishEventAsync<T>(string topic, T eventData, string? key = null, CancellationToken cancellationToken = default);
    Task PublishEventAsync<T>(string topic, string key, T eventData, CancellationToken cancellationToken = default);
}