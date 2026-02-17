using DistributedTaskFramework.Core.Abstractions;
using StackExchange.Redis;

namespace DistributedTaskFramework.Infrastructure.Redis;

public sealed class RedisDistributedLockManager : IDistributedLockManager
{
    private readonly IDatabase _database;

    public RedisDistributedLockManager(IConnectionMultiplexer connectionMultiplexer)
    {
        _database = connectionMultiplexer.GetDatabase();
    }

    public async Task<IAsyncDisposable?> TryAcquireAsync(string lockName, TimeSpan holdFor, CancellationToken cancellationToken)
    {
        var token = Guid.NewGuid().ToString("N");
        var key = BuildKey(lockName);
        var acquired = await _database.StringSetAsync(key, token, holdFor, when: When.NotExists);
        if (!acquired)
        {
            throw new InvalidOperationException($"Could not acquire distributed lock '{lockName}'.");
        }

        return new RedisLock(_database, key, token);
    }

    private static string BuildKey(string lockName) => $"jobs:locks:{lockName}";

    private sealed class RedisLock : IAsyncDisposable
    {
        private readonly IDatabase _database;
        private readonly string _key;
        private readonly string _token;

        public RedisLock(IDatabase database, string key, string token)
        {
            _database = database;
            _key = key;
            _token = token;
        }

        public async ValueTask DisposeAsync()
        {
            const string script = "if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('del', KEYS[1]) else return 0 end";
            await _database.ScriptEvaluateAsync(script, new RedisKey[] { _key }, new RedisValue[] { _token });
        }
    }
}
