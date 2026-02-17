namespace DistributedTaskFramework.Core.Models;

public sealed record JobResult(bool Success, string? Message = null)
{
    public static JobResult Ok(string? message = null) => new(true, message);
    public static JobResult Failed(string? message = null) => new(false, message);
}
