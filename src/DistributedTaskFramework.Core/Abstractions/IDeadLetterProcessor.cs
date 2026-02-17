using DistributedTaskFramework.Core.Models;

namespace DistributedTaskFramework.Core.Abstractions;

public interface IDeadLetterProcessor
{
    Task StoreAsync(JobEnvelope envelope, Exception exception, CancellationToken cancellationToken);
}
