namespace Kafka.Consumer.Configuration;

public class KafkaConsumerSettings
{
    public string BootstrapServers { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;
    public string AutoOffsetReset { get; set; } = "Earliest";
    public bool EnableAutoCommit { get; set; } = true;
    public int StatisticsIntervalMs { get; set; } = 5000;
    public int SessionTimeoutMs { get; set; } = 6000;
    public int AutoCommitIntervalMs { get; set; } = 5000;
    public bool EnablePartitionEof { get; set; } = false;
    public bool AllowAutoCreateTopics { get; set; } = true;
    public string SecurityProtocol { get; set; } = string.Empty;
    public string SaslMechanism { get; set; } = string.Empty;
    public string SaslUsername { get; set; } = string.Empty;
    public string SaslPassword { get; set; } = string.Empty;
    public string SslCaLocation { get; set; } = string.Empty;
    public string BrokerVersionFallback { get; set; } = string.Empty;
    public int ApiVersionFallbackMs { get; set; } = 0;
    public string BrokerAddressFamily { get; set; } = string.Empty;
    public List<string> Topics { get; set; } = [];
}