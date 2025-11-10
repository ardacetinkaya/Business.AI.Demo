using AI.Agent.Custom;
using Business.Application.Extensions;
using Business.Application.Services;
using Business.Domain.Repositories;
using Business.Domain.Services;
using Business.Infrastructure.Database;
using Business.Infrastructure.Extensions;
using MCP.Server.Tools;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
// Configure PostgreSQL DbContext
var connectionString = builder.Configuration.GetConnectionString("Checkouts");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Orders connection string is not configured.");
}
builder.AddRedisDistributedCache(connectionName: "cache");

builder.Services.AddApplication();
builder.Services.AddDatabase(connectionString,builder.Environment);

Microsoft.Agents.AI.AIAgent agent = new CustomAgent();
var thread = agent.GetNewThread();
var tool = McpServerTool.Create(agent.AsAIFunction(
    new Microsoft.Extensions.AI.AIFunctionFactoryOptions
    {
        Name = "accountant_does_financial_calculations",
        Description = "Accountant Agent that can perform financial calculations for net amounts for orders payments",
    }, thread
));

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<RandomNumberTools>()
    .WithTools<PaymentsTools>()
    .WithTools([tool]);

builder.AddServiceDefaults();

var app = builder.Build();

app.MapMcp();
app.Run();
