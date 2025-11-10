using Business.Domain.Entities;

namespace Business.Domain.Repositories;

public interface IPaymentMethodFeeRepository
{
    Task<IEnumerable<PaymentMethodFee>> GetAllActiveFeesAsync(CancellationToken cancellationToken = default);
    Task<PaymentMethodFee?> GetFeeByPaymentMethodAsync(string paymentMethod, CancellationToken cancellationToken = default);
    Task<PaymentMethodFee> CreateOrUpdateFeeAsync(PaymentMethodFee fee, CancellationToken cancellationToken = default);
    Task<bool> DeactivateFeeAsync(string paymentMethod, CancellationToken cancellationToken = default);
}