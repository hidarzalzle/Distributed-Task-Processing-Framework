using DistributedTaskFramework.Application.Dispatching;
using DistributedTaskFramework.Application.Reliability;
using DistributedTaskFramework.Core.Abstractions;
using DistributedTaskFramework.Infrastructure.RabbitMq;
using DistributedTaskFramework.Infrastructure.Redis;
using DistributedTaskFramework.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Trace;
using RabbitMQ.Client;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect("localhost:6379"));
builder.Services.AddSingleton<IConnection>(_ => new ConnectionFactory
{
    HostName = "localhost",
    UserName = "guest",
    Password = "guest",
    DispatchConsumersAsync = true
}.CreateConnection());

builder.Services.AddSingleton<IIdempotencyStore, RedisIdempotencyStore>();
builder.Services.AddSingleton<IDistributedLockManager, RedisDistributedLockManager>();
builder.Services.AddSingleton<IRetryPolicy>(_ => new ExponentialBackoffRetryPolicy(
    maxRetries: 8,
    baseDelay: TimeSpan.FromSeconds(2),
    maxDelay: TimeSpan.FromMinutes(15)));

builder.Services.AddSingleton<IJobScheduler, RabbitMqJobScheduler>();
builder.Services.AddSingleton<IDeadLetterProcessor, RabbitMqDeadLetterProcessor>();
builder.Services.AddSingleton<IJobEnvelopeSerializer>(_ => new JsonJobEnvelopeSerializer(new[] { typeof(EmailJobV1) }));

builder.Services.AddSingleton<IJobDispatcher, JobDispatcher>();
builder.Services.AddSingleton<IJobHandler<EmailJobV1>, EmailJobV1Handler>();
builder.Services.AddHostedService<WorkerHost>();

builder.Services.AddOpenTelemetry().WithTracing(tracer => tracer.AddSource("DistributedTaskFramework.JobDispatcher"));

await builder.Build().RunAsync();
