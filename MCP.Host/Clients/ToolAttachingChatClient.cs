using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace MCP.Host.Clients;

public sealed class ToolAttachingChatClient(IChatClient inner, IMcpToolProvider provider) : DelegatingChatClient(inner)
{
    private static IList<AITool> MergeTools(IList<AITool>? existing, IReadOnlyList<AITool> mcp)
    {
        if (existing is null || existing.Count == 0)
            return mcp.ToList();

        // Append only those MCP tools that aren't already present by name
        var names = new HashSet<string>(existing.Select(t => t.Name), StringComparer.OrdinalIgnoreCase);
        foreach (var aiTool in mcp)
            if (!names.Contains(aiTool.Name))
                existing.Add(aiTool);

        return existing;
    }
    
    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var chatOptions = options ?? new ChatOptions();
        var mcpTools = await provider.GetToolsAsync(cancellationToken);
        chatOptions.Tools = MergeTools(chatOptions.Tools, mcpTools);
        
        return await base.GetResponseAsync(messages, chatOptions, cancellationToken);
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var chatOptions = options ?? new ChatOptions();
        var mcpTools = await provider.GetToolsAsync(cancellationToken);
        chatOptions.Tools = MergeTools(chatOptions.Tools, mcpTools);

        await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken))
            yield return update;
    }


}