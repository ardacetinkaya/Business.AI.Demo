using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;

namespace MCP.Host.Clients;

public interface IMcpToolProvider
{
    Task<IReadOnlyList<AITool>> GetToolsAsync(CancellationToken ct = default);
}

public sealed class McpToolProvider(McpHttpClient mcp, ILogger<McpToolProvider> logger) : IMcpToolProvider
{
    private volatile IReadOnlyList<AITool>? _cached;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private DateTimeOffset _lastAttempt = DateTimeOffset.MinValue;
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(5);

    public async Task<IReadOnlyList<AITool>> GetToolsAsync(CancellationToken ct = default)
    {
        // Serve cached tools if present and recent
        var cached = _cached;
        if (cached is not null && DateTimeOffset.UtcNow - _lastAttempt < RefreshInterval)
            return cached;

        if (!await _gate.WaitAsync(0, ct))
            return _cached ?? []; // another refresh in-flight

        try
        {
            _lastAttempt = DateTimeOffset.UtcNow;

            // Ensure MCP session
            try
            {
                await mcp.InitializeAsync(ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "MCP initialize failed");
            }

            // Try list tools
            var tools = await mcp.ListToolsAsync(ct);

            // Convert to AIFunction delegates that forward to MCP tools/call
            var aiTools = tools.Select(tool =>
                {
                    logger.LogInformation("Setting MCP tool {ToolName}", tool.Name);
                    return AIFunctionFactory.Create(
                        async (AIFunctionArguments args, CancellationToken cancellationToken) =>
                        {
                            var dict = args.ToDictionary(kv => kv.Key, kv => kv.Value);

                            var result = await mcp.CallToolAsync(
                                tool.Name,
                                JsonSerializer.SerializeToElement(dict),
                                cancellationToken);
                            var rawText = result.GetRawText();
                            logger.LogInformation("MCP tool {ToolName}: {RawText}", tool.Name, rawText);
                            return rawText;
                        },
                        new AIFunctionFactoryOptions
                        {
                            Name = tool.Name,
                            Description = tool.Description ?? "External MCP tool"
                        });
                }
            ).Cast<AITool>().ToList();

            _cached = aiTools;
            return _cached;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Fetching MCP tools failed; proceeding with no tools.");
            return _cached ?? []; // keep app alive even if MCP is down
        }
        finally
        {
            _gate.Release();
        }
    }
}