using Kafka.Consumer.Configuration;
using Kafka.Consumer.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var hostBuilder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.SetBasePath(Directory.GetCurrentDirectory())
              .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
              .AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        // Configure strongly-typed settings
        services.Configure<KafkaSettings>(
            context.Configuration.GetSection(KafkaSettings.SectionName));

        // Register services
        services.AddSingleton<IConsumerService, ConsumerService>();
        
        // Register background services
        services.AddHostedService<KafkaConsumerService>();
    })
    .ConfigureLogging((context, logging) =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.AddConfiguration(context.Configuration.GetSection("Logging"));
    })
    .UseConsoleLifetime();

var host = hostBuilder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();

try
{
    logger.LogInformation("Starting Kafka Consumer Application...");
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
