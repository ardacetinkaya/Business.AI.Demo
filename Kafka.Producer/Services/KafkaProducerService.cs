using Confluent.Kafka;
using Kafka.Producer.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Kafka.Producer.Services;

public class KafkaProducerService : IProducerService, IDisposable
{
    private readonly KafkaSettings _kafkaSettings;
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducerService> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };
    private bool _disposed = false;

    public KafkaProducerService(
        ILogger<KafkaProducerService> logger,
        IOptions<KafkaSettings> kafkaSettings)
    {
        _logger = logger;
        _kafkaSettings = kafkaSettings.Value;
        _producer = CreateProducer();
    }

    public async Task PublishEventAsync<T>(string topic, T eventData, string? key = null, CancellationToken cancellationToken = default)
    {
        await PublishEventAsync(topic, key ?? string.Empty, eventData, cancellationToken);
    }

    public async Task PublishEventAsync<T>(string topic, string key, T eventData, CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(KafkaProducerService));
        }

        try
        {
            var serializedData = JsonSerializer.Serialize(eventData, _jsonOptions);
            
            _logger.LogDebug("Publishing event to topic {Topic} with key {Key}", topic, key);

            var message = new Message<string, string>
            {
                Key = key,
                Value = serializedData,
                Timestamp = new Timestamp(DateTime.UtcNow)
            };

            var deliveryResult = await _producer.ProduceAsync(topic, message, cancellationToken);

            _logger.LogInformation(
                "Successfully published event to {Topic}:{Partition} at offset {Offset}. Key: {Key}",
                deliveryResult.Topic,
                deliveryResult.Partition,
                deliveryResult.Offset,
                key);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex,
                "Failed to publish event to topic {Topic} with key {Key}. Error: {ErrorCode} - {ErrorReason}",
                topic, key, ex.Error.Code, ex.Error.Reason);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error publishing event to topic {Topic} with key {Key}",
                topic, key);
            throw;
        }
    }

    private IProducer<string, string> CreateProducer()
    {
        var config = CreateProducerConfig();
        
        var producer = new ProducerBuilder<string, string>(config)
            .SetValueSerializer(Serializers.Utf8)
            .SetKeySerializer(Serializers.Utf8)
            .SetErrorHandler((_, error) =>
            {
                _logger.LogError("Kafka producer error: {ErrorCode} - {Reason}", error.Code, error.Reason);
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
                
                _logger.Log(logLevel, "Kafka producer log: {Message}", logMessage.Message);
            })
            .Build();

        _logger.LogInformation("Kafka producer created for bootstrap servers: {BootstrapServers}", 
            config.BootstrapServers);

        return producer;
    }

    private ProducerConfig CreateProducerConfig()
    {
        var settings = _kafkaSettings.Producer;
        
        var config = new ProducerConfig
        {
            BootstrapServers = settings.BootstrapServers,
            MaxInFlight = settings.MaxInFlight,
            EnableIdempotence = settings.EnableIdempotence,
            LingerMs = settings.LingerMs,
            BatchSize = settings.BatchSize
        };

        config.Acks = ParseEnum<Acks>(settings.Acks);
        config.CompressionType = ParseEnum<CompressionType>(settings.CompressionType);
        config.SecurityProtocol = ParseEnum<SecurityProtocol>(settings.SecurityProtocol);
        config.SaslMechanism = ParseEnum<SaslMechanism>(settings.SaslMechanism);

        if (!string.IsNullOrEmpty(settings.SaslUsername))
            config.SaslUsername = settings.SaslUsername;
        if (!string.IsNullOrEmpty(settings.SaslPassword))
            config.SaslPassword = settings.SaslPassword;
        if (!string.IsNullOrEmpty(settings.SslCaLocation))
            config.SslCaLocation = settings.SslCaLocation;

        _logger.LogInformation("Kafka producer configuration created for bootstrap servers: {BootstrapServers}", 
            config.BootstrapServers);

        return config;
    }

    private static T? ParseEnum<T>(string? value) where T : struct, Enum =>
        !string.IsNullOrEmpty(value) && Enum.TryParse<T>(value, ignoreCase: true, out var result) 
            ? result 
            : null;

    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                _producer?.Flush(TimeSpan.FromSeconds(10));
                _producer?.Dispose();
                _logger.LogInformation("Kafka producer disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing Kafka producer");
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}