using DistributedTaskFramework.Core.Models;

namespace DistributedTaskFramework.Core.Abstractions;

public interface IJobScheduler
{
    Task ScheduleAsync(JobEnvelope envelope, DateTimeOffset runAtUtc, CancellationToken cancellationToken);
}
