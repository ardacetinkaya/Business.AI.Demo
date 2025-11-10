using Business.Domain.Entities;
using Business.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Business.Infrastructure.Database;

public class OrderRepository(
    CheckoutsDbContext context,
    ILogger<OrderRepository> logger) : IOrderRepository
{
    public async Task<Order> SaveOrderAsync(Order order, CancellationToken cancellationToken = default)
    {
        return await SaveOrderAsync(order, saveChanges: true, cancellationToken);
    }

    public async Task<Order> SaveOrderAsync(Order order, bool saveChanges, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if order already exists to prevent duplicates
            var existingOrder = await context.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderId == order.OrderId || o.EventId == order.EventId, cancellationToken);

            if (existingOrder != null)
            {
                logger.LogWarning("Order with OrderId {OrderId} or EventId {EventId} already exists. Skipping save.",
                    order.OrderId, order.EventId);
                return existingOrder;
            }

            order.ProcessedAt = DateTime.UtcNow;
            
            context.Orders.Add(order);
            
            if (saveChanges)
            {
                await context.SaveChangesAsync(cancellationToken);
            }

            logger.LogInformation("Successfully saved order {OrderId} with ID {Id}", order.OrderId, order.Id);
            return order;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving order {OrderId}", order.OrderId);
            throw;
        }
    }

    public async Task<Order?> GetOrderByOrderIdAsync(string orderId, CancellationToken cancellationToken = default)
    {
        return await context.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);
    }

    public async Task<Order?> GetOrderByEventIdAsync(string eventId, CancellationToken cancellationToken = default)
    {
        return await context.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.EventId == eventId, cancellationToken);
    }

    public async Task<Order?> GetOrderWithPaymentAsync(string orderId, string eventId, CancellationToken cancellationToken = default)
    {
        return await context.Orders
            .Include(o => o.Payment)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderId == orderId || o.EventId == eventId, cancellationToken);
    }

    public async Task<IEnumerable<Order>> GetOrdersByCustomerIdAsync(string customerId, CancellationToken cancellationToken = default)
    {
        return await context.Orders
            .AsNoTracking()
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Order>> GetRecentOrdersAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        return await context.Orders
            .AsNoTracking()
            .OrderByDescending(o => o.ProcessedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}