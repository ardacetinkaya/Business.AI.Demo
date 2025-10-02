using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Kafka.Consumer.Services;

public class ConsumerService(ILogger<ConsumerService> logger) : IConsumerService
{
    public async Task ProcessMessageAsync(ConsumeResult<string, string> message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Processing message from topic {Topic}, partition {Partition}, offset {Offset}",
            message.Topic, message.Partition, message.Offset);

        try
        {
            // Log message details
            logger.LogInformation("Message details: Key={Key}, Timestamp={Timestamp}, Value={Value}",
                message.Message.Key ?? "null",
                message.Message.Timestamp.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                message.Message.Value);

            // Simulate some processing work
            await Task.Delay(100, cancellationToken);

            logger.LogInformation("Successfully processed message from {Topic}:{Partition}:{Offset}",
                message.Topic, message.Partition, message.Offset);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing message from {Topic}:{Partition}:{Offset}",
                message.Topic, message.Partition, message.Offset);
            
            throw;
        }
    }
}