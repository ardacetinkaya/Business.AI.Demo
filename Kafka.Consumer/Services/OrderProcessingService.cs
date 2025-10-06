using Kafka.Consumer.Data;
using Kafka.Consumer.Models;
using Kafka.Consumer.Repositories;
using Microsoft.Extensions.Logging;

namespace Kafka.Consumer.Services;

public interface IOrderProcessingService
{
    Task<(Order order, Payment payment)> ProcessOrderWithPaymentAsync(Order order, Payment payment, CancellationToken cancellationToken = default);
}

public class OrderProcessingService(
    CheckoutsDbContext context, 
    IOrderRepository orderRepository,
    IPaymentRepository paymentRepository,
    ILogger<OrderProcessingService> logger) : IOrderProcessingService
{
    public async Task<(Order order, Payment payment)> ProcessOrderWithPaymentAsync(Order order, Payment payment,CancellationToken cancellationToken = default)
    {
        // Check if order already exists using repository
        var existingOrder = await orderRepository.GetOrderWithPaymentAsync(order.OrderId, order.EventId, cancellationToken);

        if (existingOrder != null)
        {
            logger.LogInformation(
                "Order with OrderId {OrderId} or EventId {EventId} already exists. Returning existing order.",
                order.OrderId, order.EventId);
            return (existingOrder, existingOrder.Payment!);
        }

        // Use transaction for atomic operation across both entities
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Set timestamps
            order.ProcessedAt = payment.CreatedAt = DateTime.UtcNow;

            // Save order without committing changes yet
            var savedOrder = await orderRepository.SaveOrderAsync(order, saveChanges: false, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            
            logger.LogInformation("Order saved in transaction: OrderId={OrderId}, ID={Id}",
                savedOrder.OrderId, savedOrder.Id);

            // Save payment without committing changes yet
            var savedPayment = await paymentRepository.SavePaymentAsync(payment, saveChanges: false, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Payment saved in transaction: OrderId={OrderId}, TransactionId={TransactionId}, ID={Id}",
                savedPayment.OrderId, savedPayment.TransactionId, savedPayment.Id);

            // Commit the transaction
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation("Transaction committed successfully for OrderId: {OrderId}", order.OrderId);

            return (savedOrder, savedPayment);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing order {OrderId}. Rolling back transaction.", order.OrderId);

            try
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogInformation("Transaction rolled back successfully for OrderId: {OrderId}", order.OrderId);
            }
            catch (Exception rollbackEx)
            {
                logger.LogError(rollbackEx, "Failed to rollback transaction for OrderId: {OrderId}", order.OrderId);
            }

            throw;
        }
    }
}