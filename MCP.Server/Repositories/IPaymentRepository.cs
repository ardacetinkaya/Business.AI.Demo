using MCP.Server.Models;

namespace MCP.Server.Repositories;

public interface IPaymentRepository
{
    Task<Payment?> GetPaymentByOrderIdAsync(string orderId, CancellationToken cancellationToken = default);
    Task<Payment?> GetPaymentByTransactionIdAsync(string transactionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Payment>> GetPaymentsByStatusAsync(string status, CancellationToken cancellationToken = default);
    Task<IEnumerable<Payment>> GetRecentPaymentsAsync(int count = 10, CancellationToken cancellationToken = default);
}