using DistributedTaskFramework.Core.Abstractions;
using DistributedTaskFramework.Core.Models;

namespace DistributedTaskFramework.Application.Reliability;

public sealed class ExponentialBackoffRetryPolicy : IRetryPolicy
{
    private readonly int _maxRetries;
    private readonly TimeSpan _baseDelay;
    private readonly TimeSpan _maxDelay;

    public ExponentialBackoffRetryPolicy(int maxRetries, TimeSpan baseDelay, TimeSpan maxDelay)
    {
        _maxRetries = maxRetries;
        _baseDelay = baseDelay;
        _maxDelay = maxDelay;
    }

    public RetryDecision Decide(JobEnvelope envelope, Exception exception)
    {
        var nextRetry = envelope.Metadata.RetryCount + 1;
        if (nextRetry > _maxRetries)
        {
            return RetryDecision.DeadLetter(poison: true);
        }

        var exponential = Math.Pow(2, nextRetry - 1);
        var jitterMs = Random.Shared.Next(100, 500);
        var proposed = TimeSpan.FromMilliseconds(_baseDelay.TotalMilliseconds * exponential + jitterMs);

        return RetryDecision.RetryAfter(proposed > _maxDelay ? _maxDelay : proposed);
    }
}
