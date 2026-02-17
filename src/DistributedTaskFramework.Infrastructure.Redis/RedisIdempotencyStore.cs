using DistributedTaskFramework.Core.Abstractions;
using StackExchange.Redis;

namespace DistributedTaskFramework.Infrastructure.Redis;

public sealed class RedisIdempotencyStore : IIdempotencyStore
{
    private readonly IDatabase _database;

    public RedisIdempotencyStore(IConnectionMultiplexer connectionMultiplexer)
    {
        _database = connectionMultiplexer.GetDatabase();
    }

    public Task<bool> TryBeginAsync(string idempotencyKey, TimeSpan ttl, CancellationToken cancellationToken)
    {
        return _database.StringSetAsync(BuildKey(idempotencyKey), "processing", ttl, when: When.NotExists);
    }

    public Task MarkCompletedAsync(string idempotencyKey, CancellationToken cancellationToken)
    {
        return _database.StringSetAsync(BuildKey(idempotencyKey), "completed", TimeSpan.FromDays(7));
    }

    private static string BuildKey(string idempotencyKey) => $"jobs:idempotency:{idempotencyKey}";
}
