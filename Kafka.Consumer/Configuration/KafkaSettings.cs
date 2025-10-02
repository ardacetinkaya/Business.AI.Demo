namespace Kafka.Consumer.Configuration;

public class KafkaSettings
{
    public const string SectionName = "Kafka";
    
    public KafkaConsumerSettings Consumer { get; set; } = new();
}