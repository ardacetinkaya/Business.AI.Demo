using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AI.Agent;

internal sealed class AccountantAgentThread : InMemoryAgentThread
{
    private readonly string _filePath;

    internal AccountantAgentThread() : base()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "thread");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "agent_thread.json");
    }

    internal AccountantAgentThread(JsonElement serializedThreadState, JsonSerializerOptions? jsonSerializerOptions = null)
        : base(serializedThreadState, jsonSerializerOptions)
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "thread");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "agent_thread.json");
    }

    protected override async Task MessagesReceivedAsync(IEnumerable<ChatMessage> newMessages, CancellationToken cancellationToken = new CancellationToken())
    {
        var serializedJson = this.Serialize(JsonSerializerOptions.Web).GetRawText();

        await File.WriteAllTextAsync(_filePath, serializedJson, cancellationToken);

        await base.MessagesReceivedAsync(newMessages, cancellationToken);
    }
}