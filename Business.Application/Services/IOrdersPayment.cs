using Business.Domain.Entities;

namespace Business.Application.Services;

public interface IOrdersPayment
{
    Task<(Order order, Payment payment)> ExecuteInTransactionAsync(Func<CancellationToken, Task<(Order order, Payment payment)>> action, CancellationToken cancellationToken = default);
}