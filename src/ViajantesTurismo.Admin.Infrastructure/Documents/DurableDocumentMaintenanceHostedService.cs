using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using SharedKernel.Npgsql;
using SharedKernel.Scheduling;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Infrastructure.Documents;

/// <summary>Dispatches the three document retention jobs through the governed PostgreSQL scheduler.</summary>
internal sealed partial class DurableDocumentMaintenanceHostedService(
    PostgreSqlJobScheduler scheduler,
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<DurableDocumentMaintenanceHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan RenewalInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan IdlePollDelay = TimeSpan.FromSeconds(5);

    private static readonly IReadOnlyDictionary<string, DocumentMaintenanceJob> Jobs = DocumentMaintenanceSchedule.Jobs
        .ToDictionary(DocumentMaintenanceSchedule.GetJobName, StringComparer.Ordinal);

    /// <summary>Registers the jobs after the governed migration made their scheduler available.</summary>
    public override async Task StartAsync(CancellationToken ct)
    {
        var firstDueAt = timeProvider.GetUtcNow();
        foreach (var job in Jobs.Keys)
        {
            await scheduler.Register(job, firstDueAt, DocumentMaintenanceSchedule.FixedDelay, ct).ConfigureAwait(false);
        }

        await base.StartAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Runs at most one slot per maintenance job. PostgreSQL coordinates slots across hosts.</summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var slotsCancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        var slots = Enumerable.Range(0, Jobs.Count)
            .Select(_ => RunSlot(slotsCancellation.Token))
            .ToArray();

        _ = await Task.WhenAny(slots).ConfigureAwait(false);
        await slotsCancellation.CancelAsync().ConfigureAwait(false);
        await Task.WhenAll(slots).ConfigureAwait(false);
    }

    private async Task RunSlot(CancellationToken ct)
    {
        using var drafts = new BatchDrain(DocumentMaintenanceSchedule.GetJobName(DocumentMaintenanceJob.Drafts));
        using var audit = new BatchDrain(DocumentMaintenanceSchedule.GetJobName(DocumentMaintenanceJob.Audit));
        using var idempotency = new BatchDrain(DocumentMaintenanceSchedule.GetJobName(DocumentMaintenanceJob.Idempotency));
        var observations = new Dictionary<DocumentMaintenanceJob, BatchDrain>
        {
            [DocumentMaintenanceJob.Drafts] = drafts,
            [DocumentMaintenanceJob.Audit] = audit,
            [DocumentMaintenanceJob.Idempotency] = idempotency,
        };

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var lease = await scheduler.Acquire(LeaseDuration, ct).ConfigureAwait(false);
                if (lease is not null)
                {
                    await RunLease(lease, observations, ct).ConfigureAwait(false);
                    continue;
                }
            }
            catch (NpgsqlException exception) when (exception.IsTransient)
            {
                SchedulerUnavailable(logger, exception.GetType().Name);
            }

            await Task.Delay(IdlePollDelay, timeProvider, ct).ConfigureAwait(false);
        }
    }

    private async Task RunLease(
        PostgreSqlJobLease lease, Dictionary<DocumentMaintenanceJob, BatchDrain> observations, CancellationToken ct)
    {
        if (!Jobs.TryGetValue(lease.JobName, out var job))
        {
            UnknownJob(logger, lease.RunId);
            await Complete(lease, PostgreSqlScheduleRunStatus.TerminalFailure, ct).ConfigureAwait(false);
            return;
        }

        using var drainCancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
        using var heartbeatCancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var heartbeat = RenewUntilFinished(lease, drainCancellation.CancelAsync, heartbeatCancellation.Token);
        var outcome = PostgreSqlScheduleRunStatus.Succeeded;
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var drain = scope.ServiceProvider.GetRequiredService<DocumentMaintenanceDrain>();
            await observations[job].Run(drain.Bind(job), drainCancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (drainCancellation.IsCancellationRequested)
        {
            // The current execution stays recoverable after its lease expires.
        }
        catch (Exception exception) when (CanReportFailure(exception))
        {
            outcome = PostgreSqlScheduleRunStatus.Failed;
            DrainFailed(logger, lease.JobName, lease.RunId, exception.GetType().FullName ?? exception.GetType().Name);
        }
        finally
        {
            await heartbeatCancellation.CancelAsync().ConfigureAwait(false);
            await heartbeat.ConfigureAwait(false);
        }

        if (!drainCancellation.IsCancellationRequested)
        {
            await Complete(lease, outcome, ct).ConfigureAwait(false);
        }
    }

    private async Task RenewUntilFinished(
        PostgreSqlJobLease lease, Func<Task> cancelDrain, CancellationToken ct)
    {
        using var timer = new PeriodicTimer(RenewalInterval, timeProvider);
        try
        {
            while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
            {
                if (!await scheduler.Renew(lease, LeaseDuration, ct).ConfigureAwait(false))
                {
                    LeaseLost(logger, lease.RunId);
                    await cancelDrain().ConfigureAwait(false);
                    return;
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (CanReportFailure(exception))
        {
            SchedulerUnavailable(logger, exception.GetType().Name);
            await cancelDrain().ConfigureAwait(false);
        }
    }

    private async Task Complete(PostgreSqlJobLease lease, PostgreSqlScheduleRunStatus outcome, CancellationToken ct)
    {
        if (!await scheduler.Complete(lease, outcome, nextDueAt: null, ct).ConfigureAwait(false))
        {
            LeaseLost(logger, lease.RunId);
        }
    }

    private static bool CanReportFailure(Exception exception) => exception is not OutOfMemoryException
        and not StackOverflowException and not ThreadAbortException;

    [LoggerMessage(3010, LogLevel.Warning, "Document maintenance could not reach its scheduler. Error type: {ErrorType}.", EventName = "MaintenanceSchedulerUnavailable")]
    private static partial void SchedulerUnavailable(ILogger logger, string errorType);

    [LoggerMessage(3011, LogLevel.Warning, "Document maintenance no longer owns execution {RunId}.", EventName = "MaintenanceSchedulerLeaseLost")]
    private static partial void LeaseLost(ILogger logger, Guid runId);

    [LoggerMessage(3012, LogLevel.Error, "Document maintenance job {JobName} failed execution {RunId}. Error type: {ErrorType}.", EventName = "MaintenanceSchedulerDrainFailed")]
    private static partial void DrainFailed(ILogger logger, string jobName, Guid runId, string errorType);

    [LoggerMessage(3013, LogLevel.Error, "Document maintenance refused unrecognized execution {RunId}.", EventName = "MaintenanceSchedulerUnknownJob")]
    private static partial void UnknownJob(ILogger logger, Guid runId);
}
