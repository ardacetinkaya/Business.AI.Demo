using Business.Domain.Entities;

namespace Business.Domain.Repositories;

public interface IPaymentRepository
{
    Task<Payment> SavePaymentAsync(Payment payment, CancellationToken cancellationToken = default);
    Task<Payment> SavePaymentAsync(Payment payment, bool saveChanges, CancellationToken cancellationToken = default);
    Task<Payment?> GetPaymentByOrderIdAsync(string orderId, CancellationToken cancellationToken = default);
    Task<Payment?> GetPaymentByTransactionIdAsync(string transactionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Payment>> GetPaymentsByStatusAsync(string status, CancellationToken cancellationToken = default);
    Task<IEnumerable<Payment>> GetRecentPaymentsAsync(int count = 10, CancellationToken cancellationToken = default);
}