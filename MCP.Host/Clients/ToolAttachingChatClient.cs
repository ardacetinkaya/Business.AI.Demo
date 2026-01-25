using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace MCP.Host.Clients;

public sealed class ToolAttachingChatClient(IChatClient inner, IMcpToolProvider provider, ILogger<ToolAttachingChatClient> logger) : DelegatingChatClient(inner)
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

    private async Task<ChatOptions> PrepareOptionsWithToolsAsync(ChatOptions? options, CancellationToken cancellationToken)
    {
        var chatOptions = options ?? new ChatOptions();
        var mcpTools = await provider.GetToolsAsync(cancellationToken);
        chatOptions.Tools = MergeTools(chatOptions.Tools, mcpTools);
        return chatOptions;
    }
    
    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var chatOptions = await PrepareOptionsWithToolsAsync(options, cancellationToken);
            
            return await base.GetResponseAsync(messages, chatOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while getting chat response. Returning error message as response.");
            
            // Return error as a chat response instead of throwing
            var errorMessage = $"An error occurred while processing your request: {ex.Message}";
            return new ChatResponse(new ChatMessage(ChatRole.Assistant, errorMessage));
        }
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ChatOptions? chatOptions = null;
        bool hasInitialError = false;
        ChatResponseUpdate? initialErrorUpdate = null;
        
        try
        {
            chatOptions = await PrepareOptionsWithToolsAsync(options, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while preparing chat options for streaming. Returning error message as response.");
            
            // Set error flag and prepare error update
            var errorMessage = $"An error occurred while preparing your request: {ex.Message}";
            initialErrorUpdate = new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                Contents = [new TextContent(errorMessage)]
            };
            hasInitialError = true;
        }
        
        // If there was an initial error, yield it and stop
        if (hasInitialError && initialErrorUpdate is not null)
        {
            yield return initialErrorUpdate;
            yield break;
        }

        IAsyncEnumerator<ChatResponseUpdate>? enumerator = null;
        bool hasError = false;
        ChatResponseUpdate? errorUpdate = null;
        
        try
        {
            enumerator = base.GetStreamingResponseAsync(messages, chatOptions, cancellationToken).GetAsyncEnumerator(cancellationToken);
            
            while (true)
            {
                ChatResponseUpdate update;
                
                try
                {
                    if (!await enumerator.MoveNextAsync())
                        break;
                    
                    update = enumerator.Current;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error occurred while streaming chat response. Returning error message as response.");
                    
                    // Set error flag and prepare error update to yield after breaking out of try block
                    var errorMessage = $"An error occurred while processing your request: {ex.Message}";
                    errorUpdate = new ChatResponseUpdate
                    {
                        Role = ChatRole.Assistant,
                        Contents = [new TextContent(errorMessage)]
                    };
                    hasError = true;
                    break;
                }
                
                yield return update;
            }
        }
        finally
        {
            if (enumerator is not null)
                await enumerator.DisposeAsync();
        }
        
        // Yield error update if there was an error
        if (hasError && errorUpdate is not null)
            yield return errorUpdate;
    }


}