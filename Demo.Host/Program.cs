using Confluent.Kafka;
using Confluent.Kafka.Admin;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
                .WithPgAdmin(pgAdmin => pgAdmin.WithHostPort(5050));

var database = postgres.AddDatabase("Orders");

var username = builder.AddParameter("username");
var password = builder.AddParameter("password");


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
            new TopicSpecification { Name = "order-events", NumPartitions = 3, ReplicationFactor = 1 }
        ]);
    }
    catch (CreateTopicsException)
    {
        throw;
    }
});



// Add the Kafka Consumer application
var consumer = builder.AddProject<Projects.Kafka_Consumer>("kafka-consumer")
    .WithEnvironment(context =>
    {
        // Additional individual connection details as environment variables
        context.EnvironmentVariables["POSTGRES_HOST"] = postgres.Resource.PrimaryEndpoint.Property(EndpointProperty.Host);
        context.EnvironmentVariables["POSTGRES_PORT"] = postgres.Resource.PrimaryEndpoint.Property(EndpointProperty.Port);
        context.EnvironmentVariables["POSTGRES_USER"] = postgres.Resource.UserNameParameter;
        context.EnvironmentVariables["POSTGRES_PASSWORD"] = postgres.Resource.PasswordParameter;
        context.EnvironmentVariables["POSTGRES_DATABASE"] = database.Resource.DatabaseName;
    })
    .WithReference(database)
    .WithReplicas(1);

// Add the Kafka Producer application  
var producer = builder.AddProject<Projects.Kafka_Producer>("kafka-producer")
    .WithReplicas(1);

// Optional: Add resource dependencies if needed
consumer.WaitFor(database);
producer.WaitFor(kafka); // Uncomment if you want producer to wait for consumer


builder.Build().Run();
