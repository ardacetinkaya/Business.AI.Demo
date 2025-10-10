using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MCP.Server.Repositories;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace MCP.Server.Tools;

internal class PaymentsTools(ILogger<PaymentsTools> logger, IPaymentRepository paymentRepository, IDistributedCache cache)
{
    [McpServerTool]
    [Description("Returns recent payment transactions from the payment system")]
    public async Task<object> GetRecentPayments([Description("Number of payments")] int count = 7)
    {
        const string cacheKey = "recent_payments";
        
        try
        {
            // Try to get from cache first
            var cachedBytes = await cache.GetAsync(cacheKey);
            if (cachedBytes != null)
            {
                logger.LogInformation("Returning cached payments");
                var cachedJson = Encoding.UTF8.GetString(cachedBytes);
                var cachedPayments = JsonSerializer.Deserialize<List<object>>(cachedJson);
                return cachedPayments ?? new List<object>();
            }

            logger.LogInformation("Retrieving recent {Count} payments from repository",count);
            var result = await paymentRepository.GetRecentPaymentsAsync(count);
            var payments = result.ToList();
            
            // Cache the result
            var json = JsonSerializer.Serialize(payments);
            var bytes = Encoding.UTF8.GetBytes(json);
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
            };
            await cache.SetAsync(cacheKey, bytes, cacheOptions);
            
            logger.LogInformation("Successfully retrieved and cached {Count} payments", payments.Count);
            return payments;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving recent payments");
            throw;
        }
    }
}