using System.Text;
using System.Text.Json;
using DistributedTaskFramework.Core.Abstractions;
using DistributedTaskFramework.Core.Models;

namespace DistributedTaskFramework.Infrastructure.RabbitMq;

public sealed class JsonJobEnvelopeSerializer : IJobEnvelopeSerializer
{
    private readonly IReadOnlyDictionary<string, Type> _jobTypes;

    public JsonJobEnvelopeSerializer(IEnumerable<Type> jobTypes)
    {
        _jobTypes = jobTypes.ToDictionary(x => x.Name, x => x);
    }

    public byte[] Serialize(JobEnvelope envelope)
    {
        var payload = new SerializedEnvelope
        {
            JobType = envelope.Job.GetType().Name,
            Job = JsonSerializer.SerializeToElement(envelope.Job, envelope.Job.GetType()),
            Metadata = envelope.Metadata,
            Headers = envelope.Headers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        };

        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
    }

    public JobEnvelope Deserialize(ReadOnlyMemory<byte> payload)
    {
        var serialized = JsonSerializer.Deserialize<SerializedEnvelope>(payload.Span)
                         ?? throw new InvalidOperationException("Unable to deserialize envelope");

        if (!_jobTypes.TryGetValue(serialized.JobType, out var jobType))
        {
            throw new InvalidOperationException($"Unknown job type: {serialized.JobType}");
        }

        var job = (Core.Abstractions.IJob?)serialized.Job.Deserialize(jobType)
                  ?? throw new InvalidOperationException("Unable to deserialize job");

        return new JobEnvelope(job, serialized.Metadata, serialized.Headers);
    }

    private sealed class SerializedEnvelope
    {
        public required string JobType { get; init; }
        public required JsonElement Job { get; init; }
        public required JobMetadata Metadata { get; init; }
        public required Dictionary<string, string> Headers { get; init; }
    }
}
