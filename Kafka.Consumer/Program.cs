using Kafka.Consumer.Configuration;
using Kafka.Consumer.Data;
using Kafka.Consumer.Repositories;
using Kafka.Consumer.Services;
using Microsoft.EntityFrameworkCore;
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

builder.Services.AddDbContext<CheckoutsDbContext>(options =>
{
    options.UseNpgsql(connectionString);
    options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
    options.EnableDetailedErrors(builder.Environment.IsDevelopment());
});

// Register repositories
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPaymentMethodFeeRepository, PaymentMethodFeeRepository>();


// Register services
builder.Services.AddScoped<IPaymentFeeCalculator, PaymentFeeCalculator>();
builder.Services.AddScoped<IOrderProcessingService, OrderProcessingService>();
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
