namespace DistributedTaskFramework.Core.Models;

public sealed record JobContext(string? TenantId, string CorrelationId, string JobId);
