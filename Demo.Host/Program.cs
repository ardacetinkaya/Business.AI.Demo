using Confluent.Kafka;
using Confluent.Kafka.Admin;

var builder = DistributedApplication.CreateBuilder(args);

var kafka = builder.AddKafka("kafka", 9093)
    .WithKafkaUI(kafkaUI => kafkaUI.WithHostPort(9100));

builder.Eventing.Subscribe<ResourceReadyEvent>(kafka.Resource, async (@event, ct) =>
{
    var cs = await kafka.Resource.ConnectionStringExpression.GetValueAsync(ct);

    var config = new AdminClientConfig
    {
        BootstrapServers = cs
    };

    using var adminClient = new AdminClientBuilder(config).Build();
    try
    {
        await adminClient.CreateTopicsAsync([
            new TopicSpecification { Name = "order-events", NumPartitions = 1, ReplicationFactor = 1 }
        ]);
    }
    catch (CreateTopicsException)
    {
        throw;
    }
});

// Add the Kafka Consumer application
var consumer = builder.AddProject<Projects.Kafka_Consumer>("kafka-consumer")
    .WithReplicas(1);

// Add the Kafka Producer application  
var producer = builder.AddProject<Projects.Kafka_Producer>("kafka-producer")
    .WithReplicas(1);

// Optional: Add resource dependencies if needed
producer.WaitFor(kafka); // Uncomment if you want producer to wait for consumer


builder.Build().Run();
