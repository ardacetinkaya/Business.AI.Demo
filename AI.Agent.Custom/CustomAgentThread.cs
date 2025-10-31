using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AI.Agent.Custom;

internal sealed class CustomAgentThread : InMemoryAgentThread
{
    private readonly string _filePath;
    private static readonly string ThreadDirectory = Path.Combine(AppContext.BaseDirectory, "thread");
    private static readonly string ThreadFilePath = Path.Combine(ThreadDirectory, "agent_thread.json");

    internal CustomAgentThread() : base()
    {
        Directory.CreateDirectory(ThreadDirectory);
        _filePath = ThreadFilePath;
    }

    internal CustomAgentThread(JsonElement serializedThreadState, JsonSerializerOptions? jsonSerializerOptions = null)
        : base(serializedThreadState, jsonSerializerOptions)
    {
        Directory.CreateDirectory(ThreadDirectory);
        _filePath = ThreadFilePath;
    }
    
    public static CustomAgentThread? LoadExistingThread()
    {
        if (!File.Exists(ThreadFilePath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(ThreadFilePath);
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(json);
            return new CustomAgentThread(jsonElement, JsonSerializerOptions.Web);
        }
        catch (Exception)
        {
            // If there's an error loading the thread, return null to create a new one
            return null;
        }
    }

    protected override async Task MessagesReceivedAsync(IEnumerable<ChatMessage> newMessages, CancellationToken cancellationToken = new CancellationToken())
    {
        var serializedJson = this.Serialize(JsonSerializerOptions.Web).GetRawText();

        await File.WriteAllTextAsync(_filePath, serializedJson, cancellationToken);

        await base.MessagesReceivedAsync(newMessages, cancellationToken);
    }
}