namespace Business.Domain.Services;

public interface IPaymentFeeCalculator
{
    Task<(decimal feePercentage, decimal feeAmount)> CalculateFeeAsync(string paymentMethod, decimal amount, CancellationToken cancellationToken = default);
}