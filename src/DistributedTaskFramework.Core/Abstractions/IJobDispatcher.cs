using DistributedTaskFramework.Core.Models;

namespace DistributedTaskFramework.Core.Abstractions;

public interface IJobDispatcher
{
    Task DispatchAsync(JobEnvelope envelope, CancellationToken cancellationToken);
}
