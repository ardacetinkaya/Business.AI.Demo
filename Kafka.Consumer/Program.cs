using Business.Infrastructure.Database;
using Business.Infrastructure.Extensions;
using Business.Application.Extensions;
using Business.Application.Services;
using Kafka.Consumer.Configuration;
using Kafka.Consumer.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// Configure strongly-typed settings
builder.Services.Configure<KafkaSettings>(
    builder.Configuration.GetSection(KafkaSettings.SectionName));

// Configure PostgreSQL DbContext
var connectionString = builder.Configuration.GetConnectionString("Checkouts");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Orders connection string is not configured.");
}

builder.AddRedisDistributedCache(connectionName: "cache");

builder.Services.AddDatabase(connectionString, builder.Environment);

builder.Services.AddOrderProcessingService();
builder.Services.AddScoped<IConsumerService, ConsumerService>();

// Register background services
builder.Services.AddHostedService<KafkaConsumerService>();

var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();

try
{
    logger.LogInformation("Starting Kafka Consumer Application...");
    
    // Ensure database is created and migrations are applied
    using (var scope = host.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<CheckoutsDbContext>();
        logger.LogInformation("Ensuring database is created...");
        await dbContext.Database.EnsureCreatedAsync();
        logger.LogInformation("Database is ready");
    }
    
    await host.RunAsync();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    logger.LogInformation("Kafka Consumer Application stopped");
}
