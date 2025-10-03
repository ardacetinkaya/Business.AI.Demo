namespace Kafka.Consumer.Services;

public interface IPaymentFeeCalculator
{
    (decimal feePercentage, decimal feeAmount) CalculateFee(string paymentMethod, decimal amount);
}

public class PaymentFeeCalculator : IPaymentFeeCalculator
{
    private static readonly Dictionary<string, decimal> PaymentMethodFees = new(StringComparer.OrdinalIgnoreCase)
    {
        { "CreditCard", 2.0m },
        { "DebitCard", 1.8m },
        { "PayPal", 1.5m },
        { "ApplePay", 1.2m },
        { "GooglePay", 1.2m },
        { "BankTransfer", 0.8m },
        { "Swish", 0.7m },
        // Default for unknown methods
        { "Unknown", 1.5m }
    };

    public (decimal feePercentage, decimal feeAmount) CalculateFee(string paymentMethod, decimal amount)
    {
        var feePercentage = PaymentMethodFees.GetValueOrDefault(paymentMethod ?? "Unknown", 1.5m);
        
        var feeAmount = Math.Round(amount * (feePercentage / 100), 2, MidpointRounding.AwayFromZero);
        
        return (feePercentage, feeAmount);
    }
}