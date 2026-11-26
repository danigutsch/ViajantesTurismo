using Microsoft.EntityFrameworkCore;
using SharedKernel.Npgsql;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Infrastructure.Documents;

/// <summary>Projects counts-only batch evidence and durable scheduler metadata.</summary>
internal sealed class DocumentMaintenanceHistoryQuery(
    AdminReadDbContext context, PostgreSqlJobScheduler scheduler, TimeProvider timeProvider) :
    IDocumentMaintenanceHistoryQuery, IDocumentMaintenanceTimelineQuery, IDocumentMaintenanceExecutionsQuery
{
    private const int MaximumRunsPerJob = 20;

    /// <inheritdoc />
    public async Task<DocumentMaintenanceHistoryDto> Read(CancellationToken ct)
    {
        var draftRuns = await ReadRecent(DocumentMaintenanceJob.Drafts, ct);
        var auditRuns = await ReadRecent(DocumentMaintenanceJob.Audit, ct);
        var idempotencyRuns = await ReadRecent(DocumentMaintenanceJob.Idempotency, ct);
        List<DocumentMaintenanceScheduleDto> schedules = [];
        foreach (var job in DocumentMaintenanceSchedule.Jobs)
        {
            schedules.Add(await ReadSchedule(job, ct));
        }

        return new DocumentMaintenanceHistoryDto(draftRuns, auditRuns, idempotencyRuns, schedules.AsReadOnly());
    }

    /// <inheritdoc />
    public async Task<GetDocumentMaintenanceTimelineDto> Read(
        DocumentMaintenanceJob job, int pageSize, DocumentMaintenanceRunCursorDto? before, CancellationToken ct)
    {
        var query = ReadBatches(job);
        if (before is { } position)
        {
            var time = position.OccurredAtUtc.UtcDateTime;
            var runId = position.RunId;
            query = query.Where(run => run.OccurredAtUtc < time
                || (run.OccurredAtUtc == time && run.RunId.CompareTo(runId) < 0));
        }

        var candidates = await NewestFirst(query).Take(pageSize + 1).ToArrayAsync(ct);
        var runs = candidates.Take(pageSize)
            .Select(run => new DocumentMaintenanceRunDto(run.OccurredAtUtc, run.PurgedCount, run.HeldCount, run.Outcome))
            .ToArray();
        var next = candidates.Length > pageSize
            ? new DocumentMaintenanceRunCursorDto(candidates[pageSize - 1].OccurredAtUtc, candidates[pageSize - 1].RunId)
            : null;
        return new GetDocumentMaintenanceTimelineDto
        {
            Runs = runs,
            Next = next,
            Schedule = await ReadSchedule(job, ct),
        };
    }

    /// <inheritdoc />
    public async Task<GetDocumentMaintenanceExecutionsDto> ReadExecutions(
        DocumentMaintenanceJob job, int pageSize, DocumentMaintenanceExecutionCursorDto? before, CancellationToken ct)
    {
        var cursor = before is null ? (PostgreSqlScheduleRunCursor?)null
            : new PostgreSqlScheduleRunCursor(before.StartedAtUtc, before.RunId);
        var page = await scheduler.ReadHistory(DocumentMaintenanceSchedule.GetJobName(job), cursor, pageSize, ct);
        var next = page.Next is { } position
            ? new DocumentMaintenanceExecutionCursorDto(position.StartedAt, position.RunId)
            : null;
        return new GetDocumentMaintenanceExecutionsDto([.. page.Runs.Select(MapExecution)], next);
    }

    private async Task<DocumentMaintenanceScheduleDto> ReadSchedule(DocumentMaintenanceJob job, CancellationToken ct)
    {
        var jobName = DocumentMaintenanceSchedule.GetJobName(job);
        var schedule = await scheduler.ReadJob(jobName, ct);
        if (schedule is null)
        {
            return new DocumentMaintenanceScheduleDto(job, null, null, null, null, null);
        }

        var active = await scheduler.ReadActive(jobName, after: null, pageSize: 1, ct);
        var recent = await scheduler.ReadHistory(jobName, after: null, pageSize: 1, ct);
        var activeRun = ReadFirst(active);
        return new DocumentMaintenanceScheduleDto(
            job, schedule.IsPaused, schedule.NextDueAt, schedule.FixedDelay, activeRun, ReadFirst(recent))
        {
            IsDue = !schedule.IsPaused && activeRun is null
                && schedule.NextDueAt is { } dueAt && dueAt <= timeProvider.GetUtcNow(),
        };
    }

    private static DocumentMaintenanceExecutionDto? ReadFirst(PostgreSqlScheduleRunPage page) =>
        page.Runs.Count > 0 ? MapExecution(page.Runs[0]) : null;

    private static DocumentMaintenanceExecutionDto MapExecution(PostgreSqlScheduleRun run) =>
        new(run.RunId, run.StartedAt, run.ScheduledFor, run.FinishedAt, run.RetryAt, run.Status switch
        {
            PostgreSqlScheduleRunStatus.Unknown => DocumentMaintenanceExecutionStatus.Unknown,
            PostgreSqlScheduleRunStatus.Running => DocumentMaintenanceExecutionStatus.Running,
            PostgreSqlScheduleRunStatus.Succeeded => DocumentMaintenanceExecutionStatus.Succeeded,
            PostgreSqlScheduleRunStatus.Cancelled => DocumentMaintenanceExecutionStatus.Cancelled,
            PostgreSqlScheduleRunStatus.Failed => DocumentMaintenanceExecutionStatus.Failed,
            PostgreSqlScheduleRunStatus.RetryScheduled => DocumentMaintenanceExecutionStatus.RetryScheduled,
            PostgreSqlScheduleRunStatus.TerminalFailure => DocumentMaintenanceExecutionStatus.TerminalFailure,
            _ => throw new ArgumentOutOfRangeException(nameof(run), run.Status, null),
        });

    private Task<DocumentMaintenanceRunDto[]> ReadRecent(DocumentMaintenanceJob job, CancellationToken ct) =>
        NewestFirst(ReadBatches(job))
            .Take(MaximumRunsPerJob)
            .Select(run => new DocumentMaintenanceRunDto(run.OccurredAtUtc, run.PurgedCount, run.HeldCount, run.Outcome))
            .ToArrayAsync(ct);

    private IQueryable<Batch> ReadBatches(DocumentMaintenanceJob job) => job switch
    {
        DocumentMaintenanceJob.Drafts => ReadRetentionBatches(DocumentRetentionPurgeKind.Drafts),
        DocumentMaintenanceJob.Audit => context.DocumentAuditPurgeRecords
            .Select(record => new Batch
            {
                RunId = record.Id,
                OccurredAtUtc = record.OccurredAtUtc,
                PurgedCount = record.PurgedCount,
                HeldCount = record.HeldCount,
                Outcome = record.Outcome.ToString(),
            }),
        DocumentMaintenanceJob.Idempotency => ReadRetentionBatches(DocumentRetentionPurgeKind.IdempotencyEntries),
        _ => throw new ArgumentOutOfRangeException(nameof(job), job, null),
    };

    private IQueryable<Batch> ReadRetentionBatches(DocumentRetentionPurgeKind kind) =>
        context.DocumentRetentionPurgeRecords.Where(record => record.Kind == kind)
            .Select(record => new Batch
            {
                RunId = record.Id,
                OccurredAtUtc = record.OccurredAtUtc,
                PurgedCount = record.PurgedCount,
                HeldCount = record.HeldCount,
                Outcome = record.Outcome.ToString(),
            });

    private static IOrderedQueryable<Batch> NewestFirst(IQueryable<Batch> query) =>
        query.OrderByDescending(run => run.OccurredAtUtc).ThenByDescending(run => run.RunId);

    private sealed class Batch
    {
        /// <summary>The retained identifier used to break timestamp ties.</summary>
        public Guid RunId
        {
            get;
            init;
        }

        /// <summary>The time when the batch evidence was recorded.</summary>
        public DateTime OccurredAtUtc
        {
            get;
            init;
        }

        /// <summary>The number of deleted records.</summary>
        public int PurgedCount
        {
            get;
            init;
        }

        /// <summary>The number of held records.</summary>
        public int HeldCount
        {
            get;
            init;
        }

        /// <summary>The recorded batch outcome.</summary>
        public required string Outcome
        {
            get;
            init;
        }
    }
}
