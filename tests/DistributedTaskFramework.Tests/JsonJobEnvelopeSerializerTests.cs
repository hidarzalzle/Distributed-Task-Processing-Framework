using DistributedTaskFramework.Core.Abstractions;
using DistributedTaskFramework.Core.Models;
using DistributedTaskFramework.Infrastructure.RabbitMq;
using Xunit;

namespace DistributedTaskFramework.Tests;

public sealed class JsonJobEnvelopeSerializerTests
{
    [Fact]
    public void Serialize_And_Deserialize_RoundTripsEnvelope()
    {
        var serializer = new JsonJobEnvelopeSerializer(new[] { typeof(TestJobV1) });
        var envelope = CreateEnvelope();

        var bytes = serializer.Serialize(envelope);
        var deserialized = serializer.Deserialize(bytes);

        var deserializedJob = Assert.IsType<TestJobV1>(deserialized.Job);
        Assert.Equal("hello", deserializedJob.Payload);
        Assert.Equal(envelope.Metadata.JobId, deserialized.Metadata.JobId);
        Assert.Equal("tenant-a", deserialized.Metadata.TenantId);
        Assert.Equal("v", deserialized.Headers["k"]);
    }

    [Fact]
    public void Deserialize_WithUnknownType_Throws()
    {
        var known = new JsonJobEnvelopeSerializer(new[] { typeof(TestJobV1) });
        var unknown = new JsonJobEnvelopeSerializer(Array.Empty<Type>());

        var payload = known.Serialize(CreateEnvelope());

        var ex = Assert.Throws<InvalidOperationException>(() => unknown.Deserialize(payload));
        Assert.Contains("Unknown job type", ex.Message);
    }

    private static JobEnvelope CreateEnvelope()
    {
        var metadata = new JobMetadata(
            JobId: Guid.NewGuid().ToString("N"),
            IdempotencyKey: Guid.NewGuid().ToString("N"),
            TenantId: "tenant-a",
            CorrelationId: Guid.NewGuid().ToString("N"),
            RetryCount: 0,
            CreatedAtUtc: DateTimeOffset.UtcNow);

        return new JobEnvelope(
            new TestJobV1("hello"),
            metadata,
            new Dictionary<string, string> { ["k"] = "v" });
    }

    private sealed record TestJobV1(string Payload) : IJob
    {
        public string JobType => "tests.serializer";
        public int Version => 1;
    }
}
