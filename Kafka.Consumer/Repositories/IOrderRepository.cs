using Kafka.Consumer.Models;

namespace Kafka.Consumer.Repositories;

public interface IOrderRepository
{
    Task<Order> SaveOrderAsync(Order order, CancellationToken cancellationToken = default);
    Task<Order> SaveOrderAsync(Order order, bool saveChanges, CancellationToken cancellationToken = default);
    Task<Order?> GetOrderByOrderIdAsync(string orderId, CancellationToken cancellationToken = default);
    Task<Order?> GetOrderByEventIdAsync(string eventId, CancellationToken cancellationToken = default);
    Task<Order?> GetOrderWithPaymentAsync(string orderId, string eventId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Order>> GetOrdersByCustomerIdAsync(string customerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Order>> GetRecentOrdersAsync(int count = 10, CancellationToken cancellationToken = default);
}