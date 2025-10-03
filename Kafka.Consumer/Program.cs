using Kafka.Consumer.Configuration;
using Kafka.Consumer.Data;
using Kafka.Consumer.Repositories;
using Kafka.Consumer.Services;
using Microsoft.EntityFrameworkCore;
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

        // Configure PostgreSQL DbContext
        var connectionString = context.Configuration.GetConnectionString("Checkouts");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Orders connection string is not configured.");
        }

        services.AddDbContext<CheckoutsDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
            options.EnableSensitiveDataLogging(context.HostingEnvironment.IsDevelopment());
            options.EnableDetailedErrors(context.HostingEnvironment.IsDevelopment());
        });

        // Register repositories
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        
        // Register services
        services.AddScoped<IPaymentFeeCalculator, PaymentFeeCalculator>();
        services.AddScoped<IOrderProcessingService, OrderProcessingService>();
        services.AddScoped<IConsumerService, ConsumerService>();
        
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
