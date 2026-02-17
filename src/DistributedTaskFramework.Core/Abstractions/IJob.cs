namespace DistributedTaskFramework.Core.Abstractions;

public interface IJob
{
    string JobType { get; }
    int Version { get; }
}
