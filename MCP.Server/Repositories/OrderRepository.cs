using MCP.Server.Data;
using MCP.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MCP.Server.Repositories;

public class OrderRepository( CheckoutsDbContext context) : IOrderRepository
{
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