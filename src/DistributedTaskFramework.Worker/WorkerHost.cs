using DistributedTaskFramework.Core.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DistributedTaskFramework.Worker;

public sealed class WorkerHost : BackgroundService
{
    private readonly IConnection _connection;
    private readonly IJobEnvelopeSerializer _serializer;
    private readonly IJobDispatcher _dispatcher;
    private readonly ILogger<WorkerHost> _logger;

    public WorkerHost(IConnection connection, IJobEnvelopeSerializer serializer, IJobDispatcher dispatcher, ILogger<WorkerHost> logger)
    {
        _connection = connection;
        _serializer = serializer;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = _connection.CreateModel();
        channel.ExchangeDeclare("jobs.main.exchange", ExchangeType.Topic, durable: true);
        channel.QueueDeclare("jobs.main.worker", durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind("jobs.main.worker", "jobs.main.exchange", "#");
        channel.BasicQos(0, 32, global: false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, args) =>
        {
            try
            {
                var envelope = _serializer.Deserialize(args.Body);
                await _dispatcher.DispatchAsync(envelope, stoppingToken);
                channel.BasicAck(args.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process message {DeliveryTag}", args.DeliveryTag);
                channel.BasicNack(args.DeliveryTag, multiple: false, requeue: false);
            }
        };

        channel.BasicConsume("jobs.main.worker", autoAck: false, consumer);
        _logger.LogInformation("Worker host started.");

        return Task.CompletedTask;
    }
}
