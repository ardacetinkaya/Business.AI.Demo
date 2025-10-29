using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AI.Agent;

internal sealed class AccountantAgentThread : InMemoryAgentThread
{
    internal AccountantAgentThread() : base() { }
    internal AccountantAgentThread(JsonElement serializedThreadState, JsonSerializerOptions? jsonSerializerOptions = null)
        : base(serializedThreadState, jsonSerializerOptions) { }

    protected override Task MessagesReceivedAsync(IEnumerable<ChatMessage> newMessages, CancellationToken cancellationToken = new CancellationToken())
    {
        return base.MessagesReceivedAsync(newMessages, cancellationToken);
    }
}