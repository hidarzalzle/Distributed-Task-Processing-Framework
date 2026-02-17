using System.Text;
using System.Text.Json;
using DistributedTaskFramework.Core.Abstractions;
using DistributedTaskFramework.Core.Models;
using RabbitMQ.Client;

namespace DistributedTaskFramework.Infrastructure.RabbitMq;

public sealed class RabbitMqJobScheduler : IJobScheduler
{
    private readonly IModel _channel;

    public RabbitMqJobScheduler(IConnection connection)
    {
        _channel = connection.CreateModel();
    }

    public Task ScheduleAsync(JobEnvelope envelope, DateTimeOffset runAtUtc, CancellationToken cancellationToken)
    {
        var delayMs = Math.Max(0, (int)(runAtUtc - DateTimeOffset.UtcNow).TotalMilliseconds);
        var exchange = "jobs.delay.exchange";
        var queue = $"jobs.delay.{delayMs}";

        _channel.ExchangeDeclare(exchange, ExchangeType.Direct, durable: true);
        _channel.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object>
            {
                ["x-message-ttl"] = delayMs,
                ["x-dead-letter-exchange"] = "jobs.main.exchange",
                ["x-dead-letter-routing-key"] = envelope.Job.JobType
            });
        _channel.QueueBind(queue, exchange, envelope.Job.JobType);

        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(envelope));
        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.CorrelationId = envelope.Metadata.CorrelationId;
        properties.MessageId = envelope.Metadata.JobId;
        properties.Headers = envelope.Headers.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);

        _channel.BasicPublish(exchange, envelope.Job.JobType, properties, bytes);
        return Task.CompletedTask;
    }
}
