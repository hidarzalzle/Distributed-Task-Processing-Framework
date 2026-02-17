using DistributedTaskFramework.Core.Abstractions;

namespace DistributedTaskFramework.Core.Models;

public sealed record JobEnvelope(
    IJob Job,
    JobMetadata Metadata,
    IDictionary<string, string> Headers)
{
    public JobEnvelope WithRetry(int retryCount, DateTimeOffset visibleAtUtc)
        => this with
        {
            Metadata = Metadata with
            {
                RetryCount = retryCount,
                VisibleAtUtc = visibleAtUtc
            }
        };
}
