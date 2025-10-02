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

            // Try to parse and log JSON if applicable
            if (TryParseJson(message.Message.Value, out var formattedJson))
            {
                logger.LogInformation("Formatted JSON message: {JsonMessage}", formattedJson);
            }

            // Simulate some processing work
            await Task.Delay(100, cancellationToken);

            // TODO: Add your business logic here
            // Examples:
            // - Save to database
            // - Call external API
            // - Transform and forward to another topic
            // - Send notifications

            logger.LogInformation("Successfully processed message from {Topic}:{Partition}:{Offset}",
                message.Topic, message.Partition, message.Offset);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing message from {Topic}:{Partition}:{Offset}",
                message.Topic, message.Partition, message.Offset);
            
            // Depending on your requirements, you might want to:
            // - Rethrow to let the consumer handle it
            // - Send to a dead letter queue
            // - Log and continue processing other messages
            throw;
        }
    }

    private bool TryParseJson(string value, out string formatted)
    {
        formatted = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(value);
            formatted = JsonSerializer.Serialize(jsonElement, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            return true;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to parse message as JSON: {Value}", value);
            return false;
        }
    }
}