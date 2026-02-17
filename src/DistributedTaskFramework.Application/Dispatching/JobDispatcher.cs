using System.Diagnostics;
using DistributedTaskFramework.Core.Abstractions;
using DistributedTaskFramework.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DistributedTaskFramework.Application.Dispatching;

public sealed class JobDispatcher : IJobDispatcher
{
    private static readonly ActivitySource ActivitySource = new("DistributedTaskFramework.JobDispatcher");

    private readonly IServiceProvider _serviceProvider;
    private readonly IIdempotencyStore _idempotencyStore;
    private readonly IDistributedLockManager _lockManager;
    private readonly IRetryPolicy _retryPolicy;
    private readonly IJobScheduler _jobScheduler;
    private readonly IDeadLetterProcessor _deadLetterProcessor;
    private readonly ILogger<JobDispatcher> _logger;

    public JobDispatcher(
        IServiceProvider serviceProvider,
        IIdempotencyStore idempotencyStore,
        IDistributedLockManager lockManager,
        IRetryPolicy retryPolicy,
        IJobScheduler jobScheduler,
        IDeadLetterProcessor deadLetterProcessor,
        ILogger<JobDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _idempotencyStore = idempotencyStore;
        _lockManager = lockManager;
        _retryPolicy = retryPolicy;
        _jobScheduler = jobScheduler;
        _deadLetterProcessor = deadLetterProcessor;
        _logger = logger;
    }

    public async Task DispatchAsync(JobEnvelope envelope, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("job.dispatch", ActivityKind.Consumer);
        activity?.SetTag("job.id", envelope.Metadata.JobId);
        activity?.SetTag("job.type", envelope.Job.JobType);
        activity?.SetTag("tenant.id", envelope.Metadata.TenantId ?? "none");
        activity?.SetTag("correlation.id", envelope.Metadata.CorrelationId);

        var acquired = await _idempotencyStore.TryBeginAsync(envelope.Metadata.IdempotencyKey, TimeSpan.FromHours(24), cancellationToken);
        if (!acquired)
        {
            _logger.LogInformation("Duplicate job skipped: {JobId}", envelope.Metadata.JobId);
            return;
        }

        try
        {
            var distributedLock = await TryAcquireLockAsync(envelope, cancellationToken);
            if (distributedLock is not null)
            {
                await using (distributedLock.ConfigureAwait(false))
                {
                    await InvokeHandlerAsync(envelope, cancellationToken);
                }
            }
            else
            {
                await InvokeHandlerAsync(envelope, cancellationToken);
            }

            await _idempotencyStore.MarkCompletedAsync(envelope.Metadata.IdempotencyKey, cancellationToken);
        }
        catch (Exception ex)
        {
            var decision = _retryPolicy.Decide(envelope, ex);
            if (decision.ShouldRetry && decision.Delay is not null)
            {
                var next = envelope.WithRetry(envelope.Metadata.RetryCount + 1, DateTimeOffset.UtcNow.Add(decision.Delay.Value));
                await _jobScheduler.ScheduleAsync(next, next.Metadata.VisibleAtUtc!.Value, cancellationToken);
                _logger.LogWarning(ex, "Job {JobId} failed and will retry in {Delay}.", envelope.Metadata.JobId, decision.Delay);
                return;
            }

            _logger.LogError(ex, "Job {JobId} failed permanently. Poison={Poison}", envelope.Metadata.JobId, decision.IsPoisonMessage);
            await _deadLetterProcessor.StoreAsync(envelope, ex, cancellationToken);
        }
    }

    private async Task<IAsyncDisposable?> TryAcquireLockAsync(JobEnvelope envelope, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(envelope.Metadata.LockKey))
        {
            return null;
        }

        return await _lockManager.TryAcquireAsync(envelope.Metadata.LockKey, TimeSpan.FromMinutes(5), cancellationToken);
    }

    private async Task InvokeHandlerAsync(JobEnvelope envelope, CancellationToken cancellationToken)
    {
        var handlerType = typeof(IJobHandler<>).MakeGenericType(envelope.Job.GetType());
        dynamic handler = _serviceProvider.GetRequiredService(handlerType);
        dynamic job = envelope.Job;

        var context = new JobContext(envelope.Metadata.TenantId, envelope.Metadata.CorrelationId, envelope.Metadata.JobId);
        JobResult result = await handler.HandleAsync(job, context, cancellationToken);

        if (!result.Success)
        {
            throw new InvalidOperationException(result.Message ?? "Job handler returned failure.");
        }
    }
}
