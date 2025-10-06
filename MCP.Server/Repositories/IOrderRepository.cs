using MCP.Server.Models;

namespace MCP.Server.Repositories;

public interface IOrderRepository
{ 
    Task<Order?> GetOrderByOrderIdAsync(string orderId, CancellationToken cancellationToken = default);
    Task<Order?> GetOrderByEventIdAsync(string eventId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Order>> GetOrdersByCustomerIdAsync(string customerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Order>> GetRecentOrdersAsync(int count = 10, CancellationToken cancellationToken = default);
}