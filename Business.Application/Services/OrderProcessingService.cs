using Business.Domain.Entities;
using Business.Domain.Repositories;
using Business.Domain.Services;
using Microsoft.Extensions.Logging;

namespace Business.Application.Services;

public class OrderProcessingService(
    IOrdersPayment context, 
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

        return await context.ExecuteInTransactionAsync(async token =>
        {
            // Set timestamps
            order.ProcessedAt = payment.CreatedAt = DateTime.UtcNow;

            // Save order without committing changes yet
            var savedOrder = await orderRepository.SaveOrderAsync(order, saveChanges: false, token);
            logger.LogInformation("Order saved in transaction: OrderId={OrderId}, ID={Id}",
                savedOrder.OrderId, savedOrder.Id);

            // Save payment without committing changes yet
            var savedPayment = await paymentRepository.SavePaymentAsync(payment, saveChanges: false, token);
            
            logger.LogInformation(
                "Payment saved in transaction: OrderId={OrderId}, TransactionId={TransactionId}, ID={Id}",
                savedPayment.OrderId, savedPayment.TransactionId, savedPayment.Id);
            
            logger.LogInformation("Transaction committed successfully for OrderId: {OrderId}", order.OrderId);

            return (savedOrder, savedPayment);
        }, cancellationToken);
    }
}