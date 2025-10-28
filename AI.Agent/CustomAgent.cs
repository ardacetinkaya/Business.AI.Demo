using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

public sealed class CustomAgent : AIAgent
{
    public override AgentThread DeserializeThread(JsonElement serializedThread, JsonSerializerOptions? jsonSerializerOptions = null)
    {
        return new CustomAgentThread(serializedThread, jsonSerializerOptions);
    }

    public override AgentThread GetNewThread()
    {
        return new CustomAgentThread();
    }

    public override async Task<AgentRunResponse> RunAsync(IEnumerable<ChatMessage> messages, AgentThread? thread = null, AgentRunOptions? options = null, CancellationToken cancellationToken = default)
    {
        thread ??= this.GetNewThread();
        List<ChatMessage> responseMessages = CloneAndToUpperCase(messages, this.DisplayName).ToList();
        await NotifyThreadOfNewMessagesAsync(thread, messages.Concat(responseMessages), cancellationToken);
        return new AgentRunResponse
        {
            AgentId = this.Id,
            ResponseId = Guid.NewGuid().ToString(),
            Messages = responseMessages
        };
    }

    public override async IAsyncEnumerable<AgentRunResponseUpdate> RunStreamingAsync(IEnumerable<ChatMessage> messages, AgentThread? thread = null, AgentRunOptions? options = null, CancellationToken cancellationToken = default)
    {
        thread ??= this.GetNewThread();
        List<ChatMessage> responseMessages = CloneAndToUpperCase(messages, this.DisplayName).ToList();
        await NotifyThreadOfNewMessagesAsync(thread, messages.Concat(responseMessages), cancellationToken);
        foreach (var message in responseMessages)
        {
            yield return new AgentRunResponseUpdate
            {
                AgentId = this.Id,
                AuthorName = this.DisplayName,
                Role = ChatRole.Assistant,
                Contents = message.Contents,
                ResponseId = Guid.NewGuid().ToString(),
                MessageId = Guid.NewGuid().ToString()
            };
        }
    }

    private static IEnumerable<ChatMessage> CloneAndToUpperCase(IEnumerable<ChatMessage> messages, string agentName) => messages.Select(x =>
    {
        var messageClone = x.Clone();
        messageClone.Role = ChatRole.Assistant;
        messageClone.MessageId = Guid.NewGuid().ToString();
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