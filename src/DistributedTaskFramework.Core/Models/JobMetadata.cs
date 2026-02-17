namespace DistributedTaskFramework.Core.Models;

public sealed record JobMetadata(
    string JobId,
    string IdempotencyKey,
    string? TenantId,
    string CorrelationId,
    int RetryCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? VisibleAtUtc = null,
    string? LockKey = null,
    TimeSpan? Timeout = null);
