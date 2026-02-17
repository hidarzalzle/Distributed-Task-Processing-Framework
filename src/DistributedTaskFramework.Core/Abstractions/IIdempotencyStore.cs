namespace DistributedTaskFramework.Core.Abstractions;

public interface IIdempotencyStore
{
    Task<bool> TryBeginAsync(string idempotencyKey, TimeSpan ttl, CancellationToken cancellationToken);
    Task MarkCompletedAsync(string idempotencyKey, CancellationToken cancellationToken);
}
