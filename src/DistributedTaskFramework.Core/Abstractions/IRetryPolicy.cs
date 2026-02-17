using DistributedTaskFramework.Core.Models;

namespace DistributedTaskFramework.Core.Abstractions;

public interface IRetryPolicy
{
    RetryDecision Decide(JobEnvelope envelope, Exception exception);
}
