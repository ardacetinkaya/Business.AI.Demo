namespace Kafka.Producer.Configuration;

public class KafkaProducerSettings
{
    public string BootstrapServers { get; set; } = string.Empty;
    public string SecurityProtocol { get; set; } = string.Empty;
    public string SaslMechanism { get; set; } = string.Empty;
    public string SaslUsername { get; set; } = string.Empty;
    public string SaslPassword { get; set; } = string.Empty;
    public string SslCaLocation { get; set; } = string.Empty;
    public string Acks { get; set; } = "All";
    public int Retries { get; set; } = 3;
    public int MaxInFlight { get; set; } = 1;
    public bool EnableIdempotence { get; set; } = true;
    public string CompressionType { get; set; } = "Snappy";
    public int LingerMs { get; set; } = 5;
    public int BatchSize { get; set; } = 16384;
}