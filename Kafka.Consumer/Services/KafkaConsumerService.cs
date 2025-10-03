using Confluent.Kafka;
using Kafka.Consumer.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kafka.Consumer.Services;

public class KafkaConsumerService(ILogger<KafkaConsumerService> logger, IOptions<KafkaSettings> kafkaSettings, IConsumerService consumerService) : BackgroundService
{
    private readonly KafkaSettings _kafkaSettings = kafkaSettings.Value;
    private IConsumer<string, string>? _consumer;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Kafka Consumer Service starting...");

        try
        {
            await StartConsumingAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Kafka Consumer Service encountered a critical error");
            throw;
        }
        finally
        {
            logger.LogInformation("Kafka Consumer Service stopped");
        }
    }

    private async Task StartConsumingAsync(CancellationToken stoppingToken)
    {
        var config = CreateConsumerConfig();

        _consumer = new ConsumerBuilder<string, string>(config)
            .SetValueDeserializer(Deserializers.Utf8)
            .SetKeyDeserializer(Deserializers.Utf8)
            .SetErrorHandler((_, error) =>
            {
                logger.LogError("Kafka consumer error: {ErrorCode} - {Reason}", error.Code, error.Reason);
            })
            .SetLogHandler((_, logMessage) =>
            {
                var logLevel = logMessage.Level switch
                {
                    SyslogLevel.Emergency or SyslogLevel.Alert or SyslogLevel.Critical => LogLevel.Critical,
                    SyslogLevel.Error => LogLevel.Error,
                    SyslogLevel.Warning => LogLevel.Warning,
                    SyslogLevel.Notice or SyslogLevel.Info => LogLevel.Information,
                    SyslogLevel.Debug => LogLevel.Debug,
                    _ => LogLevel.Information
                };

                logger.Log(logLevel, "Kafka log: {Message}", logMessage.Message);
            })
            .Build();

        var topics = _kafkaSettings.Consumer.Topics;
        _consumer.Subscribe(topics);

        logger.LogInformation("Subscribed to topics: {Topics}", string.Join(", ", topics));
        logger.LogInformation("Waiting for messages...");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(TimeSpan.FromMilliseconds(1000));

                    if (consumeResult == null) continue;
                    
                    await consumerService.ProcessMessageAsync(consumeResult, stoppingToken);

                    // Manual commit if auto-commit is disabled
                    if (!_kafkaSettings.Consumer.EnableAutoCommit)
                    {
                        _consumer.Commit(consumeResult);
                        logger.LogDebug("Manually committed offset for {Topic}:{Partition}:{Offset}",
                            consumeResult.Topic, consumeResult.Partition, consumeResult.Offset);
                    }
                }
                catch (ConsumeException ex)
                {
                    logger.LogError(ex, "Consume error: {ErrorCode} - {Reason}", ex.Error.Code, ex.Error.Reason);
                    
                    if (ex.Error.IsFatal)
                    {
                        logger.LogCritical("Fatal consumer error, stopping service");
                        break;
                    }
                }
                catch (OperationCanceledException)
                {
                    logger.LogInformation("Consumer operation was cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unexpected error during message consumption");
                    await Task.Delay(5000, stoppingToken); // Wait before retrying
                }
            }
        }
        finally
        {
            try
            {
                _consumer?.Close();
                logger.LogInformation("Kafka consumer closed gracefully");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error closing Kafka consumer");
            }
        }
    }

    private ConsumerConfig CreateConsumerConfig()
    {
        var settings = _kafkaSettings.Consumer;

        var config = new ConsumerConfig
        {
            BootstrapServers = settings.BootstrapServers,
            GroupId = settings.GroupId,
            EnableAutoCommit = settings.EnableAutoCommit,
            StatisticsIntervalMs = settings.StatisticsIntervalMs,
            SessionTimeoutMs = settings.SessionTimeoutMs,
            AutoCommitIntervalMs = settings.AutoCommitIntervalMs,
            EnablePartitionEof = settings.EnablePartitionEof,
            AllowAutoCreateTopics = settings.AllowAutoCreateTopics,
            AutoOffsetReset = ParseEnum<AutoOffsetReset>(settings.AutoOffsetReset),
            SecurityProtocol = ParseEnum<SecurityProtocol>(settings.SecurityProtocol),
            SaslMechanism = ParseEnum<SaslMechanism>(settings.SaslMechanism),
            BrokerAddressFamily = ParseEnum<BrokerAddressFamily>(settings.BrokerAddressFamily)
        };
        
        if (!string.IsNullOrEmpty(settings.SaslUsername))
            config.SaslUsername = settings.SaslUsername;
        if (!string.IsNullOrEmpty(settings.SaslPassword))
            config.SaslPassword = settings.SaslPassword;
        if (!string.IsNullOrEmpty(settings.SslCaLocation))
            config.SslCaLocation = settings.SslCaLocation;
        if (!string.IsNullOrEmpty(settings.BrokerVersionFallback))
            config.BrokerVersionFallback = settings.BrokerVersionFallback;

        // Configure numeric properties
        if (settings.ApiVersionFallbackMs > 0)
            config.ApiVersionFallbackMs = settings.ApiVersionFallbackMs;

        logger.LogInformation("Kafka consumer configuration created for bootstrap servers: {BootstrapServers}", 
            config.BootstrapServers);

        return config;
    }

    private static T? ParseEnum<T>(string? value) where T : struct, Enum =>
        !string.IsNullOrEmpty(value) && Enum.TryParse<T>(value, ignoreCase: true, out var result) 
            ? result 
            : null;

    public override void Dispose()
    {
        _consumer?.Dispose();
        base.Dispose();
    }
}