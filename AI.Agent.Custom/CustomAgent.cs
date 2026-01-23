using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AI.Agent.Custom;

public sealed class CustomAgent : AIAgent
{
    public override ValueTask<AgentThread> DeserializeThreadAsync(JsonElement serializedThread, JsonSerializerOptions? jsonSerializerOptions = null, CancellationToken cancellationToken = default)
    {
        return new ValueTask<AgentThread>(new CustomAgentThread(serializedThread, jsonSerializerOptions));
    }

    public override ValueTask<AgentThread> GetNewThreadAsync(CancellationToken cancellationToken = default)
    {
        return new ValueTask<AgentThread>(CustomAgentThread.LoadExistingThread() ?? new CustomAgentThread());
    }

    protected override async Task<AgentResponse> RunCoreAsync(IEnumerable<ChatMessage> messages, AgentThread? thread = null, AgentRunOptions? options = null, CancellationToken cancellationToken = default)
    {
        thread ??= await this.GetNewThreadAsync(cancellationToken);
        IEnumerable<ChatMessage> chatMessages = messages as ChatMessage[] ?? messages.ToArray();
        List<ChatMessage> responseMessages = CloneAndToUpperCase(chatMessages, this.Name).ToList();
        
        return new AgentResponse(responseMessages);
    }

    protected override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(IEnumerable<ChatMessage> messages, AgentThread? thread = null, AgentRunOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        thread ??= await this.GetNewThreadAsync(cancellationToken);
        IEnumerable<ChatMessage> chatMessages = messages as ChatMessage[] ?? messages.ToArray();
        List<ChatMessage> responseMessages = CloneAndToUpperCase(chatMessages, this.Name).ToList();
        
        foreach (var message in responseMessages)
        {
            foreach (var content in message.Contents)
            {
                yield return new AgentResponseUpdate
                {
                    AuthorName = this.Name,
                    Role = ChatRole.Assistant,
                    Contents = [content]
                };
            }
        }
    }

    private static IEnumerable<ChatMessage> CloneAndToUpperCase(IEnumerable<ChatMessage> messages, string agentName) => messages.Select(x =>
    {
        var messageClone = x.Clone();
        messageClone.Role = ChatRole.Assistant;
        messageClone.AuthorName = agentName;
        messageClone.Contents = x.Contents.Select(c => c is TextContent tc ? new TextContent(tc.Text.ToUpperInvariant())
        {
            AdditionalProperties = tc.AdditionalProperties,
            Annotations = tc.Annotations,
            RawRepresentation = tc.RawRepresentation
        } : c).ToList();
        return messageClone;
    });
}