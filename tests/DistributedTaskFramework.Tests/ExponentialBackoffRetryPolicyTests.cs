using DistributedTaskFramework.Application.Reliability;
using DistributedTaskFramework.Core.Abstractions;
using DistributedTaskFramework.Core.Models;
using Xunit;

namespace DistributedTaskFramework.Tests;

public sealed class ExponentialBackoffRetryPolicyTests
{
    [Fact]
    public void Decide_WhenUnderMaxRetries_ReturnsRetryWithDelay()
    {
        var policy = new ExponentialBackoffRetryPolicy(maxRetries: 3, baseDelay: TimeSpan.FromSeconds(1), maxDelay: TimeSpan.FromSeconds(30));
        var envelope = CreateEnvelope(retryCount: 1);

        var decision = policy.Decide(envelope, new InvalidOperationException("boom"));

        Assert.True(decision.ShouldRetry);
        Assert.NotNull(decision.Delay);
        Assert.False(decision.IsPoisonMessage);
        Assert.InRange(decision.Delay!.Value.TotalMilliseconds, 2000, 2499);
    }

    [Fact]
    public void Decide_WhenOverMaxRetries_ReturnsDeadLetterPoison()
    {
        var policy = new ExponentialBackoffRetryPolicy(maxRetries: 2, baseDelay: TimeSpan.FromSeconds(1), maxDelay: TimeSpan.FromSeconds(30));
        var envelope = CreateEnvelope(retryCount: 2);

        var decision = policy.Decide(envelope, new Exception("fatal"));

        Assert.False(decision.ShouldRetry);
        Assert.True(decision.IsPoisonMessage);
        Assert.Null(decision.Delay);
    }

    private static JobEnvelope CreateEnvelope(int retryCount)
    {
        var metadata = new JobMetadata(
            JobId: Guid.NewGuid().ToString("N"),
            IdempotencyKey: Guid.NewGuid().ToString("N"),
            TenantId: "tenant-a",
            CorrelationId: Guid.NewGuid().ToString("N"),
            RetryCount: retryCount,
            CreatedAtUtc: DateTimeOffset.UtcNow);

        return new JobEnvelope(new TestJob("payload"), metadata, new Dictionary<string, string>());
    }

    private sealed record TestJob(string Value) : IJob
    {
        public string JobType => "test.job";
        public int Version => 1;
    }
}
