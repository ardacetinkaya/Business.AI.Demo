using AI.Agent.Custom;
using MCP.Server.Data;
using MCP.Server.Repositories;
using MCP.Server.Tools;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Agents.AI;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
// Configure PostgreSQL DbContext
var connectionString = builder.Configuration.GetConnectionString("Checkouts");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Orders connection string is not configured.");
}
builder.AddRedisDistributedCache(connectionName: "cache");
// Register repositories
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();

builder.Services.AddDbContext<CheckoutsDbContext>(options =>
{
    options.UseNpgsql(connectionString);
    options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
    options.EnableDetailedErrors(builder.Environment.IsDevelopment());
});

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
