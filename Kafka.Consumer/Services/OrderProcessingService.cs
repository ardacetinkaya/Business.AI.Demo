using Kafka.Consumer.Data;
using Kafka.Consumer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kafka.Consumer.Services;

public interface IOrderProcessingService
{
    Task<(Order order, Payment payment)> ProcessOrderWithPaymentAsync(Order order, Payment payment, CancellationToken cancellationToken = default);
}

public class OrderProcessingService(CheckoutsDbContext context, ILogger<OrderProcessingService> logger) : IOrderProcessingService
{
    public async Task<(Order order, Payment payment)> ProcessOrderWithPaymentAsync(Order order, Payment payment,CancellationToken cancellationToken = default)
    {
        var existingOrder = await context.Orders
            .Include(o => o.Payment)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderId == order.OrderId || o.EventId == order.EventId, cancellationToken);

        if (existingOrder != null)
        {
            logger.LogInformation(
                "Order with OrderId {OrderId} or EventId {EventId} already exists. Returning existing order.",
                order.OrderId, order.EventId);
            return (existingOrder, existingOrder.Payment!);
        }


        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            order.ProcessedAt = payment.CreatedAt = DateTime.UtcNow;

            context.Orders.Add(order);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Order saved in transaction: OrderId={OrderId}, ID={Id}",
                order.OrderId, order.Id);

            context.Payments.Add(payment);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Payment saved in transaction: OrderId={OrderId}, TransactionId={TransactionId}, ID={Id}",
                payment.OrderId, payment.TransactionId, payment.Id);

            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation("Transaction committed successfully for OrderId: {OrderId}", order.OrderId);

            return (order, payment);
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