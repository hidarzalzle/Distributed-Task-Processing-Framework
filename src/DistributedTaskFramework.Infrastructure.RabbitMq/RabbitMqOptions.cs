namespace DistributedTaskFramework.Infrastructure.RabbitMq;

public sealed class RabbitMqOptions
{
    public required string HostName { get; init; }
    public required string UserName { get; init; }
    public required string Password { get; init; }
    public required string MainExchange { get; init; }
    public required string QueueName { get; init; }
}
