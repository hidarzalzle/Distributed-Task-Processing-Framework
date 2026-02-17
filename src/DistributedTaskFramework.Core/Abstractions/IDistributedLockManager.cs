namespace DistributedTaskFramework.Core.Abstractions;

public interface IDistributedLockManager
{
    Task<IAsyncDisposable?> TryAcquireAsync(string lockName, TimeSpan holdFor, CancellationToken cancellationToken);
}
