using System.Text.Json;
using Confluent.Kafka;
using Kafka.Consumer.Models;
using Kafka.Consumer.Repositories;
using Microsoft.Extensions.Logging;

namespace Kafka.Consumer.Services;

public interface IConsumerService
{
    Task ProcessMessageAsync(ConsumeResult<string, string> message, CancellationToken cancellationToken = default);
}
public class ConsumerService(
    ILogger<ConsumerService> logger,
    IOrderProcessingService orderProcessingService,
    IPaymentFeeCalculator feeCalculator) : IConsumerService
{
    public async Task ProcessMessageAsync(ConsumeResult<string, string> message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Processing message from topic {Topic}, partition {Partition}, offset {Offset}",
            message.Topic, message.Partition, message.Offset);

        try
        {
            // Log basic message info
            logger.LogInformation("Message details: Key={Key}, Timestamp={Timestamp}",
                message.Message.Key ?? "null",
                message.Message.Timestamp.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss"));

            // Deserialize the JSON message to OrderSubmittedEventDto
            var orderEvent = JsonSerializer.Deserialize<OrderSubmittedEventDto>(message.Message.Value, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (orderEvent == null)
            {
                logger.LogWarning("Failed to deserialize message value to OrderSubmittedEventDto");
                return;
            }

            // Map to Order and Payment entities
            var order = MapToOrder(orderEvent);
            var payment = MapToPayment(orderEvent, feeCalculator);
            
            // Process order and payment in a single transaction
            var (savedOrder, savedPayment) = await orderProcessingService.ProcessOrderWithPaymentAsync(order, payment, cancellationToken);
                
            logger.LogInformation("Successfully processed order {OrderId} and payment {TransactionId} from {Topic}:{Partition}:{Offset}",
                savedOrder.OrderId, savedPayment.TransactionId, message.Topic, message.Partition, message.Offset);

            // Simulate some additional processing work
            await Task.Delay(100, cancellationToken);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Error deserializing message from {Topic}:{Partition}:{Offset}. Value: {Value}",
                message.Topic, message.Partition, message.Offset, message.Message.Value);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Business logic error processing message from {Topic}:{Partition}:{Offset}",
                message.Topic, message.Partition, message.Offset);
            // Don't rethrow - this prevents infinite retry of duplicate messages
            return;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing message from {Topic}:{Partition}:{Offset}",
                message.Topic, message.Partition, message.Offset);
            throw;
        }
    }

    private static Order MapToOrder(OrderSubmittedEventDto orderEvent)
    {
        return new Order
        {
            OrderId = orderEvent.OrderId,
            CustomerId = orderEvent.CustomerId,
            CustomerEmail = orderEvent.CustomerEmail,
            OrderDate = orderEvent.OrderDate,
            TotalAmount = orderEvent.TotalAmount,
            Currency = orderEvent.Currency,
            Status = orderEvent.Status,
            ShippingFirstName = orderEvent.ShippingAddress.FirstName,
            ShippingLastName = orderEvent.ShippingAddress.LastName,
            ShippingStreet = orderEvent.ShippingAddress.Street,
            ShippingCity = orderEvent.ShippingAddress.City,
            ShippingPostalCode = orderEvent.ShippingAddress.PostalCode,
            ShippingCountry = orderEvent.ShippingAddress.Country,
            EventId = orderEvent.EventId,
            EventTimestamp = orderEvent.EventTimestamp,
            
            // Serialize items as JSON for storage
            ItemsJson = JsonSerializer.Serialize(orderEvent.Items)
        };
    }

    private static Payment MapToPayment(OrderSubmittedEventDto orderEvent, IPaymentFeeCalculator feeCalculator)
    {
        // Calculate fee based on payment method and order amount
        var (feePercentage, feeAmount) = feeCalculator.CalculateFee(orderEvent.Payment.PaymentMethod, orderEvent.TotalAmount);

        return new Payment
        {
            OrderId = orderEvent.OrderId,
            PaymentMethod = orderEvent.Payment.PaymentMethod,
            PaymentProvider = orderEvent.Payment.PaymentProvider,
            TransactionId = orderEvent.Payment.TransactionId,
            Status = orderEvent.Payment.Status,
            Amount = orderEvent.TotalAmount,
            FeePercentage = feePercentage,
            FeeAmount = feeAmount,
            ProcessedAt = orderEvent.Payment.ProcessedAt
        };
    }
}