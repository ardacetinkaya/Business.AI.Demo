using System.Text.Json;

namespace MCP.Host.Clients;

public sealed class McpHttpClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly Uri _endpoint;
    private const string ProtocolVersion = "2025-06-18";
    private int _id = 0;

    public McpHttpClient(HttpClient http, string endpoint)
    {
        _http = http;
        _endpoint = new Uri(endpoint);

        // REQUIRED by spec: Accept must include BOTH types
        // (comma-separated; many servers 406 if one is missing)
        _http.DefaultRequestHeaders.Accept.Clear();
        _http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        _http.DefaultRequestHeaders.Accept.ParseAdd("text/event-stream");
    }

    private HttpRequestMessage NewMessage(object payload)
    {
        var msg = new HttpRequestMessage(HttpMethod.Post, _endpoint)
        {
            Content = JsonContent.Create(payload) // sets Content-Type: application/json
        };
        // After initialize, this header MUST be present on all requests
        msg.Headers.TryAddWithoutValidation("MCP-Protocol-Version", ProtocolVersion);
        return msg;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        var req = new
        {
            jsonrpc = "2.0",
            id = Interlocked.Increment(ref _id),
            method = "initialize",
            @params = new
            {
                protocolVersion = ProtocolVersion,
                capabilities = new { },
                clientInfo = new { name = "dotnet-mcp-host", version = "0.1" }
            }
        };

        using var message = NewMessage(req);
        using var response = await _http.SendAsync(message, ct);
        response.EnsureSuccessStatusCode();
    }

    public record McpTool(string Name, string? Description, JsonElement InputSchema);

    public async Task<IReadOnlyList<McpTool>> ListToolsAsync(CancellationToken ct = default)
    {
        var request = new
        {
            jsonrpc = "2.0", 
            id = Interlocked.Increment(ref _id), 
            method = "tools/list", 
            @params = new { }
        };
        
        using var message = NewMessage(request);
        using var response = await _http.SendAsync(message, ct);
        response.EnsureSuccessStatusCode();

        using var reader = new StreamReader(await response.Content.ReadAsStreamAsync(ct));
        JsonElement? toolsElement = null;
        while (await reader.ReadLineAsync(ct) is { } line)
        {
            if (line.StartsWith("data:"))
            {
                var json = line["data:".Length..].Trim();
                var doc = JsonDocument.Parse(json);
                toolsElement = doc.RootElement.GetProperty("result").GetProperty("tools");
                break;
            }


        }

        if(toolsElement == null) 
            return [];
        
        return toolsElement.Value
            .EnumerateArray()
            .Select(t => new McpTool(
                t.GetProperty("name").GetString()!,
                t.TryGetProperty("description", out var d) ? d.GetString() : null,
                t.GetProperty("inputSchema")
            ))
            .ToList();
    }

    public async Task<JsonElement> CallToolAsync(string name, JsonElement arguments, CancellationToken ct = default)
    {
        var request = new
        {
            jsonrpc = "2.0", 
            id = Interlocked.Increment(ref _id), 
            method = "tools/call", 
            @params = new { 
                name, 
                arguments 
            }
        };
        using var message = NewMessage(request);
        using var response = await _http.SendAsync(message, ct);
        response.EnsureSuccessStatusCode();

        using var reader = new StreamReader(await response.Content.ReadAsStreamAsync(ct));
        JsonElement? toolResult = null;
        while (await reader.ReadLineAsync(ct) is { } line)
        {
            if (line.StartsWith("data:"))
            {
                var json = line["data:".Length..].Trim();
                var doc = JsonDocument.Parse(json);
                toolResult = doc.RootElement;
                break;
            }
        }

        if(toolResult == null) 
            return new JsonElement();
        
        return toolResult.Value;
    }
    
    private static bool IsEventStream(string? mediaType) => string.Equals(mediaType, "text/event-stream", StringComparison.OrdinalIgnoreCase);
    
    public void Dispose() => _http.Dispose();
}