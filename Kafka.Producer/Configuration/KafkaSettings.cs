namespace Kafka.Producer.Configuration;

public class KafkaSettings
{
    public const string SectionName = "Kafka";
    
    public KafkaProducerSettings Producer { get; set; } = new();
    public required KafkaTopicsSettings Topics { get; init; } = new();
}

public class KafkaTopicsSettings
{
    public string OrderEvents => "order-events";
}