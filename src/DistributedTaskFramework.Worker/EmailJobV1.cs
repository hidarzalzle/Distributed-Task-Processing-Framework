using DistributedTaskFramework.Core.Abstractions;
using DistributedTaskFramework.Core.Models;

namespace DistributedTaskFramework.Worker;

public sealed record EmailJobV1(string Recipient, string Subject, string Body) : IJob
{
    public string JobType => "notifications.email";
    public int Version => 1;
}

public sealed class EmailJobV1Handler : IJobHandler<EmailJobV1>
{
    public Task<JobResult> HandleAsync(EmailJobV1 job, JobContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(JobResult.Ok($"Email sent to {job.Recipient} for tenant {context.TenantId}."));
    }
}
