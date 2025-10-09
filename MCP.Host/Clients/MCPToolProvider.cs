using System.Text.Json;
using Microsoft.Extensions.AI;

namespace MCP.Host.Clients;

public interface IMcpToolProvider
{
    Task<IReadOnlyList<AITool>> GetToolsAsync(CancellationToken ct = default);
}

public record McpToolParameter(string Name, string? Description, bool Required, string Type);

public sealed class McpToolProvider(McpHttpClient mcp, ILogger<McpToolProvider> logger) : IMcpToolProvider
{
    private volatile IReadOnlyList<AITool>? _cached;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private DateTimeOffset _lastAttempt = DateTimeOffset.MinValue;
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(5);

    private AITool CreateMcpTool(McpHttpClient.McpTool tool)
    {
        // Parse the input schema to understand parameters
        var parameterInfo = ExtractParameterInfo(tool.InputSchema);

        var wrapper = new GeneralToolWrapper(tool, mcp, logger, parameterInfo);
        var dynamicMethod = new Func<object[], CancellationToken, Task<string>>(wrapper.CallWithDynamicParams);

        return AIFunctionFactory.Create(dynamicMethod, new AIFunctionFactoryOptions
        {
            Name = tool.Name,
            Description = CreateEnhancedDescription(tool, parameterInfo)
        });
    }

    private static string CreateEnhancedDescription(McpHttpClient.McpTool tool, List<McpToolParameter> parameterInfo)
    {
        var description = tool.Description ?? "External MCP tool";

        if (parameterInfo.Count <= 0) return description;

        description += "\n\nParameters:";

        return parameterInfo.Aggregate(description, (current, param) =>
            current + $"\n- {param.Name} ({param.Type}): {param.Description}{(param.Required ? " (required)" : " (optional)")}");
    }


    private static List<McpToolParameter> ExtractParameterInfo(JsonElement inputSchema)
    {
        var parameters = new List<McpToolParameter>();

        if (inputSchema.TryGetProperty("properties", out var properties))
        {
            var requiredParams = new HashSet<string>();
            if (inputSchema.TryGetProperty("required", out var requiredArray))
            {
                foreach (var req in requiredArray.EnumerateArray())
                {
                    if (req.GetString() is { } reqParam)
                        requiredParams.Add(reqParam);
                }
            }

            foreach (var property in properties.EnumerateObject())
            {
                var name = property.Name;
                var description = property.Value.TryGetProperty("description", out var desc)
                    ? desc.GetString() ?? $"Parameter {name}"
                    : $"Parameter {name}";
                var type = property.Value.TryGetProperty("type", out var typeElement)
                    ? typeElement.GetString() ?? "string"
                    : "string";

                parameters.Add(new McpToolParameter(name, description, Required: requiredParams.Contains(name), type));
            }
        }

        return parameters;
    }

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
                    return CreateMcpTool(tool);
                }
            ).ToList();

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

file class GeneralToolWrapper(McpHttpClient.McpTool tool, McpHttpClient mcp, ILogger logger, List<McpToolParameter> parameterInfo)
{
    public async Task<string> CallWithDynamicParams(object[] parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            var args = new Dictionary<string, object>();

            // Map positional parameters to named parameters based on schema
            for (var i = 0; i < Math.Min(parameters.Length, parameterInfo.Count); i++)
            {
                var param = parameterInfo[i];
                var value = parameters[i];

                // Convert value to correct type based on schema
                args[param.Name] = ConvertValue(value, param.Type);
            }

            // Check for missing required parameters
            var missingRequired = new List<string>();
            foreach (var param in parameterInfo)
            {
                if (param.Required && !args.ContainsKey(param.Name))
                {
                    missingRequired.Add($"{param.Name} ({param.Description})");
                }
            }

            if (missingRequired.Count > 0)
            {
                return $"Error: Missing required parameters: {string.Join(", ", missingRequired)}";
            }

            logger.LogInformation("Calling MCP tool {ToolName} with arguments: {Args}",
                tool.Name, JsonSerializer.Serialize(args));

            var result = await mcp.CallToolAsync(tool.Name, JsonSerializer.SerializeToElement(args), cancellationToken);
            var rawText = result.GetRawText();

            logger.LogInformation("MCP tool {ToolName} response: {RawText}", tool.Name, rawText);
            return rawText;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calling MCP tool {ToolName}", tool.Name);
            return $"Error calling tool {tool.Name}: {ex.Message}";
        }
    }

    private static object ConvertValue(object value, string targetType)
    {
        return targetType switch
        {
            "integer" when value is string str && int.TryParse(str, out var intVal) => intVal,
            "integer" when value is double d => (int)d,
            "integer" when value is float f => (int)f,
            "number" when value is string str && double.TryParse(str, out var doubleVal) => doubleVal,
            "number" when value is int i => (double)i,
            "boolean" when value is string str && bool.TryParse(str, out var boolVal) => boolVal,
            "string" when value is not string => value.ToString() ?? string.Empty,
            _ => value
        };
    }
}