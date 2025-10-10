using System.Globalization;
using System.Text;
using Kafka.Consumer.Repositories;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Kafka.Consumer.Services;

public interface IPaymentFeeCalculator
{
    Task<(decimal feePercentage, decimal feeAmount)> CalculateFeeAsync(string paymentMethod, decimal amount, CancellationToken cancellationToken = default);
}

public class PaymentFeeCalculator(
    IPaymentMethodFeeRepository feeRepository,
    IDistributedCache cache,
    ILogger<PaymentFeeCalculator> logger) : IPaymentFeeCalculator
{
    private const string CacheKeyPrefix = "payment_method_fee:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(10);
    
    public async Task<(decimal feePercentage, decimal feeAmount)> CalculateFeeAsync(string paymentMethod, decimal amount, CancellationToken cancellationToken = default)
    {
        var feePercentage = await GetFeePercentageAsync(paymentMethod, cancellationToken);
        var feeAmount = Math.Round(amount * (feePercentage / 100), 2, MidpointRounding.AwayFromZero);
        
        return (feePercentage, feeAmount);
    }

    private async Task<decimal> GetFeePercentageAsync(string paymentMethod, CancellationToken cancellationToken)
    {
        var normalizedPaymentMethod = paymentMethod ?? "Unknown";
        var cacheKey = $"{CacheKeyPrefix}{normalizedPaymentMethod}";
        var fee = 0m;

        var cachedBytes = await cache.GetAsync(cacheKey, cancellationToken);
        if (cachedBytes != null)
        {
            var cachedString = Encoding.UTF8.GetString(cachedBytes);
            fee = decimal.Parse(cachedString, CultureInfo.InvariantCulture);
            logger.LogDebug("Retrieved fee for {PaymentMethod} from cache: {Fee}%", normalizedPaymentMethod, fee);
        }
        
        try
        {
            var feeEntity = await feeRepository.GetFeeByPaymentMethodAsync(normalizedPaymentMethod, cancellationToken);
            
            if (feeEntity != null)
            {
                var bytes = Encoding.UTF8.GetBytes(feeEntity.FeePercentage.ToString(CultureInfo.InvariantCulture));
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = CacheExpiration
                };
                await cache.SetAsync(cacheKey, bytes, cacheOptions, cancellationToken);
                logger.LogDebug("Retrieved fee for {PaymentMethod} from database: {Fee}%", normalizedPaymentMethod, feeEntity.FeePercentage);
                fee = feeEntity.FeePercentage;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve fee for {PaymentMethod} from database, falling back to default", normalizedPaymentMethod);
        }
        
        return fee;
    }

    public void ClearCache()
    {
        logger.LogInformation("Cache clear requested - restart application or wait for cache expiration");
    }
}