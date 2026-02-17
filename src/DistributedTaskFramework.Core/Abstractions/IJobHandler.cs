using DistributedTaskFramework.Core.Models;

namespace DistributedTaskFramework.Core.Abstractions;

public interface IJobHandler<in TJob> where TJob : IJob
{
    Task<JobResult> HandleAsync(TJob job, JobContext context, CancellationToken cancellationToken);
}
