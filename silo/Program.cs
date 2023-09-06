using Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Serialization;
using Orleans.Storage;
using Silo;
using StackExchange.Redis;
using Testcontainers.DynamoDb;
using Testcontainers.Redis;

var redisContainer = new RedisBuilder().Build();
var dynamoDbContainer = new DynamoDbBuilder().Build();
await redisContainer.StartAsync();
await dynamoDbContainer.StartAsync();

var useRedis = !args.Contains("--dynamodb");
var useJson = args.Contains("--json");

var builder = Host.CreateDefaultBuilder(args)
    .UseOrleans(siloBuilder =>
    {
        siloBuilder.UseLocalhostClustering();
        if (useRedis)
        {
            siloBuilder.AddRedisGrainStorage("TestStorage", builder =>
            {
                builder.Configure<IServiceProvider>((options, services) =>
                {
                    if (useJson)
                    {
                        options.GrainStorageSerializer = new JsonGrainStorageSerializer(services.GetRequiredService<OrleansJsonSerializer>());
                    }
                    else
                    {
                        options.GrainStorageSerializer = new OrleansGrainStorageSerializer(services.GetRequiredService<Serializer>());
                    }
                    options.ConfigurationOptions = ConfigurationOptions.Parse(redisContainer.GetConnectionString() + ",abortConnect=false");
                });
            });
        }
        else
        {
            siloBuilder.AddDynamoDBGrainStorage("TestStorage", builder =>
            {
                builder.Configure<IServiceProvider>((options, services) =>
                {
                    if (useJson)
                    {
                        options.GrainStorageSerializer = new JsonGrainStorageSerializer(services.GetRequiredService<OrleansJsonSerializer>());
                    }
                    else
                    {
                        options.GrainStorageSerializer = new OrleansGrainStorageSerializer(services.GetRequiredService<Serializer>());
                    }
                    options.TableName = $"orleans-redis-serialization-bug-{Guid.NewGuid()}";
                    options.Service = dynamoDbContainer.GetConnectionString();
                });
            });
        }
    })
    .UseConsoleLifetime();
var host = builder.Build();
await host.StartAsync();

Console.WriteLine($"Running test with {(useRedis ? "Redis" : "DynamoDB")} storage and {(useJson ? "JSON" : "Orleans")} serialization");

var clusterClient = host.Services.GetRequiredService<IClusterClient>();
var statefulGrain = clusterClient.GetGrain<IStatefulGrain>(Guid.NewGuid());

await statefulGrain.SetState(new StatefulState { Value = "test value", SomeType = new SomeType{Value = Guid.NewGuid()}});

var currentState = await statefulGrain.GetState();
Console.WriteLine($"Grain state before reactivating grain: {currentState.Value}/{currentState.SomeType.Value}");

await statefulGrain.ShutdownGrain();

currentState = await statefulGrain.GetState();
Console.WriteLine($"Grain state after reactivating grain: {currentState.Value}/{currentState.SomeType.Value}");

await host.StopAsync();
await redisContainer.StopAsync();