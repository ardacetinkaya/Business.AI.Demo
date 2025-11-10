using Business.Domain.Services;
using Microsoft.Extensions.Logging;

namespace MCP.Server.Tools;

internal class PaymentsTools(ILogger<PaymentsTools> logger, IPaymentsService paymentsService)
{
    [McpServerTool]
    [Description("Returns recent payment transactions from the payment system")]
    public async Task<object> GetRecentPayments([Description("Number of payments")] int count = 7)
    {
        return await paymentsService.GetRecentPayments(count);
    }
}