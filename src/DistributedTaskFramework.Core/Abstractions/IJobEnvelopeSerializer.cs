using DistributedTaskFramework.Core.Models;

namespace DistributedTaskFramework.Core.Abstractions;

public interface IJobEnvelopeSerializer
{
    byte[] Serialize(JobEnvelope envelope);
    JobEnvelope Deserialize(ReadOnlyMemory<byte> payload);
}
