using Business.Domain.Entities;

namespace Business.Domain.Services;

public interface IOrderProcessingService
{
    Task<(Order order, Payment payment)> ProcessOrderWithPaymentAsync(Order order, Payment payment, CancellationToken cancellationToken = default);
}