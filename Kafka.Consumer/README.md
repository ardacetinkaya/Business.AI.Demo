# Kafka Consumer Application

A .NET console application for consuming messages from Apache Kafka topics using the Confluent.Kafka library, built with Dependency Injection, structured logging, and background services.

## Architecture

The application follows modern .NET patterns and best practices:

### **Services Architecture**
- **`KafkaConsumerService`**: Background service that handles Kafka message consumption
- **`MessageProcessor`**: Service responsible for processing individual messages
- **Dependency Injection**: All services are registered and injected through the DI container
- **Structured Logging**: Uses `ILogger<T>` throughout the application
- **Configuration**: Strongly-typed configuration using the Options pattern

### **Project Structure**
```
KafkaConsumerApp/
├── Configuration/
│   └── KafkaSettings.cs          # Strongly-typed configuration models
├── Models/
│   └── OrderEvents.cs            # E-commerce event models
├── Services/
│   ├── IConsumerService.cs      # Message processing interface
│   ├── ConsumerService.cs       # Message processing implementation
│   ├── IProducerService.cs  # Kafka producer interface
│   ├── KafkaProducerService.cs   # Kafka producer implementation
│   ├── KafkaConsumerService.cs   # Background service for Kafka consumption
│   └── OrderEventGeneratorService.cs # Service generating sample order events
├── Program.cs                    # Application entry point with DI setup
├── appsettings.json              # Configuration file
└── README.md                     # This file
```

## Prerequisites

- .NET 10 RC1 or later
- Access to a Kafka cluster (local or remote)

## Configuration

The application uses `appsettings.json` for configuration with strongly-typed settings:

### **Kafka Settings**
```json
{
  "Kafka": {
    "Consumer": {
      "BootstrapServers": "your-kafka-broker:9092",
      "GroupId": "kafka-consumer-app-group",
      "AutoOffsetReset": "Earliest",
      "EnableAutoCommit": true,
      "Topics": ["order-events", "topic2"],
      "SecurityProtocol": "SaslSsl",
      "SaslMechanism": "Plain",
      "SaslUsername": "your-api-key",
      "SaslPassword": "your-api-secret"
    },
    "Producer": {
      "BootstrapServers": "",  // Leave empty to use consumer settings
      "Acks": "All",
      "EnableIdempotence": true,
      "CompressionType": "Snappy"
    },
    "Topics": {
      "OrderEvents": "order-events",
      "PaymentEvents": "payment-events", 
      "InventoryEvents": "inventory-events"
    }
  }
}
```

### **Logging Configuration**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "KafkaConsumerApp.Services": "Debug",
      "Microsoft": "Warning"
    }
  }
}
```

## Usage

1. **Configure your settings** in `appsettings.json`
2. **Run the application**:
   ```bash
   dotnet run
   ```

The application will:
- Start as a hosted service
- Subscribe to configured topics for consuming messages
- Generate sample e-commerce order events every 30-60 seconds
- Publish events to the `order-events` topic
- Process and consume messages using the injected `IConsumerService`
- Log all activities with structured logging
- Handle graceful shutdown on Ctrl+C

## Features

- ✅ **Dependency Injection**: Clean service architecture with DI container
- ✅ **Background Services**: Consumer and event generator run as hosted services
- ✅ **Kafka Producer**: Service for publishing events to Kafka topics
- ✅ **Event Generation**: Automatic generation of realistic e-commerce order events
- ✅ **Structured Logging**: Uses `ILogger<T>` with configurable log levels
- ✅ **Strongly-typed Configuration**: Type-safe configuration with Options pattern
- ✅ **E-commerce Events**: Complete order submitted event models with items, shipping, payment
- ✅ **Graceful Shutdown**: Proper cleanup and resource disposal
- ✅ **Error Handling**: Comprehensive error handling with proper logging
- ✅ **Testable**: Services are easily unit testable through interfaces
- ✅ **Configurable Topics**: Multiple topics support via configuration
- ✅ **Security Support**: Full SASL/SSL configuration support

## Customization

### **Adding Custom Message Processing**
Implement your business logic in `ConsumerService.ProcessMessageAsync`:

```csharp
public async Task ProcessMessageAsync(ConsumeResult<string, string> message, CancellationToken cancellationToken = default)
{
    // Your custom logic here
    // e.g., save to database, call APIs, transform data
}
```

### **Adding New Services**
1. Create your service interface and implementation
2. Register in `Program.cs`:
```csharp
services.AddSingleton<IYourService, YourService>();
```
3. Inject into other services as needed

### **Custom Message Processing Service**
Replace the default `ConsumerService` with your own:

```csharp
services.AddSingleton<IConsumerService, YourCustomConsumerService>();
```

### **Configuration**
All configuration is centralized in `appsettings.json`:
- Update topics in the `Topics` array
- Modify Kafka connection settings
- Adjust logging levels per namespace
- Add environment-specific configurations

## Environment Variables

You can override configuration using environment variables:
```bash
export Kafka__Consumer__BootstrapServers="localhost:9092"
export Kafka__Consumer__GroupId="my-group"
dotnet run
```

## Logging

The application uses structured logging with different log levels:
- **Information**: General application flow
- **Debug**: Detailed diagnostic information
- **Warning**: Potentially harmful situations
- **Error**: Error events that allow the application to continue
- **Critical**: Critical error events that cause termination

Configure logging levels in `appsettings.json` under the `Logging` section.

## Dependencies

- `Confluent.Kafka`: Kafka client library
- `Microsoft.Extensions.Hosting`: Background service support
- `Microsoft.Extensions.Configuration.Json`: JSON configuration
- `Microsoft.Extensions.Configuration.Binder`: Configuration binding
- `Microsoft.Extensions.Logging.Console`: Console logging provider