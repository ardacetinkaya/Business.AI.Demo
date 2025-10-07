using Kafka.Producer.Configuration;
using Kafka.Producer.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// Configure strongly-typed settings
builder.Services.Configure<KafkaSettings>(
    builder.Configuration.GetSection(KafkaSettings.SectionName));

// Register services
builder.Services.AddSingleton<IProducerService, KafkaProducerService>();

// Register background services
builder.Services.AddHostedService<OrderEventGeneratorService>();

var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();

try
{
    logger.LogInformation("Starting Kafka Producer Application...");
    await host.RunAsync();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    logger.LogInformation("Kafka Producer Application stopped");
}
