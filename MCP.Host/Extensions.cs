using MCP.Host.Clients;
using Microsoft.Extensions.AI;

namespace MCP.Host;

public static class Extensions
{
    public static IServiceCollection AddMcpToolProvider(this IServiceCollection services, IHostApplicationBuilder builder)
    {
        services.AddHttpClient("mcp"); // basic client; McpHttpClient sets headers itself
        services.AddSingleton<McpHttpClient>(sp =>
        {
            var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient("mcp");
            return new McpHttpClient(http, builder.Configuration["MCPServer:Endpoint"] ?? throw new InvalidOperationException("Missing configuration: MCPServer:Endpoint."));
        });

        builder.Services.AddSingleton<IMcpToolProvider, McpToolProvider>();
        return services;
    }
    
    public static ChatClientBuilder AddMcpTools(this ChatClientBuilder builder)
    {
        builder.Use((inner, sp) => new ToolAttachingChatClient(inner, sp.GetRequiredService<IMcpToolProvider>()));
        return builder;
    }
}