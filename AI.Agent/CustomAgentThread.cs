using System.Text.Json;
using Microsoft.Agents.AI;

internal sealed class CustomAgentThread : InMemoryAgentThread
{
    internal CustomAgentThread() : base() { }
    internal CustomAgentThread(JsonElement serializedThreadState, JsonSerializerOptions? jsonSerializerOptions = null)
        : base(serializedThreadState, jsonSerializerOptions) { }
}
