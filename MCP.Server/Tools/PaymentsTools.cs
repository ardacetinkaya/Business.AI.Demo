using System.Text.Json;
using System.Threading.Tasks;
using MCP.Server.Repositories;
using Microsoft.Extensions.Logging;

namespace MCP.Server.Tools;

internal class PaymentsTools(ILogger<PaymentsTools> logger, IPaymentRepository paymentRepository)
{
    [McpServerTool]
    [Description("Returns recent payment transactions from the payment system")]
    public async Task<object> GetRecentPayments()
    {
        try
        {
            logger.LogInformation("Retrieving recent payments from repository");
            var result = await paymentRepository.GetRecentPaymentsAsync(2);
            if (result == null)
            {
                logger.LogInformation("No payments found");
                return new List<object>();
            }
            
            var payments = result.ToList();
            logger.LogInformation("Successfully retrieved {Count} payments", payments.Count);
            return payments;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving recent payments");
            throw;
        }
    }
}