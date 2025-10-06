using Confluent.Kafka;
using Confluent.Kafka.Admin;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
                .WithPgAdmin(pgAdmin =>
                {
                    pgAdmin.WithHostPort(5050);
                });
var cache = builder.AddValkey("cache");
var database = postgres.AddDatabase("Checkouts");

var kafka = builder.AddKafka("kafka", 9093)
    .WithKafkaUI(ui =>
    {
        ui.WithHostPort(9100);
    });

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



var consumer = builder.AddProject<Projects.Kafka_Consumer>("kafka-consumer")
    .WithReference(database)
    .WithEnvironment(context =>
    {
        // Additional individual connection details as environment variables
        context.EnvironmentVariables["Kafka:Consumer:BootstrapServers"] = kafka.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None);
    })
    .WithReplicas(1)
    .WaitFor(kafka)
    .WaitFor(database);

var producer = builder.AddProject<Projects.Kafka_Producer>("kafka-producer")
    .WithEnvironment(context =>
    {
        // Additional individual connection details as environment variables
        context.EnvironmentVariables["Kafka:Producer:BootstrapServers"] = kafka.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None);
    })
    .WithReplicas(1)
    .WaitFor(kafka);

var mcpServer = builder.AddProject<Projects.MCP_Server>("mcp-server")
    .WithReference(database)
    .WithReference(cache)
    .WithHttpEndpoint(5001)
    .WithReplicas(1)
    .WaitFor(database)
    .WaitFor(producer);


var githubModelsToken = builder.AddParameter("githubmodels-token");
var mcpHost = builder.AddProject<Projects.MCP_Host>("mcp-host")
    .WithEnvironment(context =>
    {
        // Additional individual connection details as environment variables
        context.EnvironmentVariables["GitHubModels:Token"] = githubModelsToken.Resource.GetValueAsync(CancellationToken.None);
        context.EnvironmentVariables["MCPServer:Endpoint"] = "http://localhost:5001";
    })
    .WithReplicas(1)
    .WaitFor(mcpServer);


builder.Build().Run();