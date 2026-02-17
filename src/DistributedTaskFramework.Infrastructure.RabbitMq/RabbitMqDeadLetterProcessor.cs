using System.Text;
using System.Text.Json;
using DistributedTaskFramework.Core.Abstractions;
using DistributedTaskFramework.Core.Models;
using RabbitMQ.Client;

namespace DistributedTaskFramework.Infrastructure.RabbitMq;

public sealed class RabbitMqDeadLetterProcessor : IDeadLetterProcessor
{
    private readonly IModel _channel;

    public RabbitMqDeadLetterProcessor(IConnection connection)
    {
        _channel = connection.CreateModel();
        _channel.ExchangeDeclare("jobs.dlq.exchange", ExchangeType.Topic, durable: true);
    }

    public Task StoreAsync(JobEnvelope envelope, Exception exception, CancellationToken cancellationToken)
    {
        var payload = new
        {
            Envelope = envelope,
            Error = exception.Message,
            ExceptionType = exception.GetType().Name,
            OccurredAtUtc = DateTimeOffset.UtcNow
        };

        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.CorrelationId = envelope.Metadata.CorrelationId;
        properties.MessageId = envelope.Metadata.JobId;

        _channel.BasicPublish("jobs.dlq.exchange", envelope.Job.JobType, properties, bytes);
        return Task.CompletedTask;
    }
}
