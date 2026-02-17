namespace DistributedTaskFramework.Core.Models;

public sealed record RetryDecision(bool ShouldRetry, TimeSpan? Delay, bool IsPoisonMessage = false)
{
    public static RetryDecision RetryAfter(TimeSpan delay) => new(true, delay);
    public static RetryDecision DeadLetter(bool poison = false) => new(false, null, poison);
}
