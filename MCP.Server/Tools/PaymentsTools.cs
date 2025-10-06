using Microsoft.Extensions.Logging;

namespace MCP.Server.Tools;

internal class PaymentsTools(ILogger<PaymentsTools> logger)
{
    [McpServerTool]
    [Description("Returns random cities or capitals from Europe")]
    public string GetRandomCity()
    {
        return "Stockholm";
    }
}