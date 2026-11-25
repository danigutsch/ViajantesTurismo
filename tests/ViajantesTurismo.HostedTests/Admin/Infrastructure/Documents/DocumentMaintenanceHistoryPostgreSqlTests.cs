using SharedKernel.Npgsql;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Infrastructure.Documents;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Documents;

[Trait(TestTraitNames.CategoryName, SharedKernel.IntegrationTesting.IntegrationTestTraitValues.DatabaseIntegrationCategory)]
[TestScope(TestScope.Integration)]
public sealed class DocumentMaintenanceHistoryPostgreSqlTests(PostgreSqlTestServerFixture fixture)
    : IClassFixture<PostgreSqlTestServerFixture>
{
    [Fact]
    public async Task Maintenance_history_reads_recent_counts_for_each_kind_independently()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var start = DateTime.UnixEpoch.AddYears(56);
        await using var scenario = await PostgreSqlDocumentStoreScenario.Create(fixture, ct);
        await scenario.MigrateTo(null, ct);
        var draftRuns = Enumerable.Range(0, 22)
            .Select(index => DocumentRetentionPurgeRecord.Create(
                DocumentRetentionPurgeKind.Drafts, index, 1, DocumentRetentionPurgeOutcome.Succeeded, start.AddDays(index)).Value)
            .ToArray();
        var idempotencyRun = DocumentRetentionPurgeRecord.Create(
            DocumentRetentionPurgeKind.IdempotencyEntries, 0, 0, DocumentRetentionPurgeOutcome.Refused, start).Value;
        var auditRun = DocumentAuditPurgeRecord.Create(
            "synthetic-purge-actor", "synthetic-purge-correlation", 4, 3, start, DocumentAuditOutcome.Succeeded, start.AddHours(1)).Value;
        await scenario.SeedMaintenanceEvidence([.. draftRuns, idempotencyRun], auditRun, ct);

        // Act
        var history = await scenario.ReadMaintenanceHistory(ct);

        // Assert
        history.Schedules.Select(schedule => schedule.Job).ToArray().ShouldBe(
            [DocumentMaintenanceJob.Drafts, DocumentMaintenanceJob.Audit, DocumentMaintenanceJob.Idempotency]);
        history.Schedules.All(schedule => schedule.IsPaused is null).ShouldBeTrue();
        history.DraftRuns.Count.ShouldBe(20);
        history.DraftRuns[0].OccurredAtUtc.ShouldBe(start.AddDays(21));
        history.DraftRuns[0].PurgedCount.ShouldBe(21);
        history.DraftRuns[0].HeldCount.ShouldBe(1);
        history.DraftRuns[^1].OccurredAtUtc.ShouldBe(start.AddDays(2));
        history.IdempotencyRuns.ShouldHaveSingleItem().Outcome.ShouldBe("Refused");
        var audit = history.AuditRuns.ShouldHaveSingleItem();
        audit.PurgedCount.ShouldBe(4);
        audit.HeldCount.ShouldBe(3);
        audit.Outcome.ShouldBe("Succeeded");
    }

    [Fact]
    public async Task Fresh_database_reports_unregistered_schedules_and_empty_maintenance_history()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await PostgreSqlDocumentStoreScenario.Create(fixture, ct);
        await scenario.MigrateTo(null, ct);

        // Act
        var history = await scenario.ReadMaintenanceHistory(ct);

        // Assert
        history.Schedules.Count.ShouldBe(3);
        history.Schedules.All(schedule => schedule.IsPaused is null && schedule.NextDueAtUtc is null
            && schedule.FixedDelay is null && schedule.ActiveRun is null && schedule.LatestRun is null
            && !schedule.IsDue).ShouldBeTrue();
        history.DraftRuns.ShouldBeEmpty();
        history.AuditRuns.ShouldBeEmpty();
        history.IdempotencyRuns.ShouldBeEmpty();
    }

    [Fact]
    public async Task Recorded_batches_remain_visible_from_a_host_that_does_not_run_background_services()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var now = new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);
        await using var scenario = await PostgreSqlDocumentStoreScenario.Create(fixture, ct);
        await scenario.MigrateTo(null, ct);
        await scenario.RunRegisteredMaintenanceBatches(now, ct);

        // Act
        var history = await scenario.ReadMaintenanceHistory(ct);

        // Assert
        var draft = history.DraftRuns.ShouldHaveSingleItem();
        var audit = history.AuditRuns.ShouldHaveSingleItem();
        var idempotency = history.IdempotencyRuns.ShouldHaveSingleItem();
        foreach (var recorded in new[] { draft, audit, idempotency })
        {
            recorded.OccurredAtUtc.ShouldBe(now.UtcDateTime);
            recorded.Outcome.ShouldBe("Succeeded");
            recorded.PurgedCount.ShouldBe(0);
            recorded.HeldCount.ShouldBe(0);
        }
    }

    [Fact]
    public async Task Startup_drains_expired_drafts_and_a_separate_reader_observes_the_next_due_time()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var now = new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);
        var expired = DocumentDraftInfrastructureTestData.CreateDraft(now.UtcDateTime.AddDays(-DocumentLimits.DraftRetentionDays - 1));
        var live = DocumentDraftInfrastructureTestData.CreateDraft(now.UtcDateTime);
        await using var scenario = await PostgreSqlDocumentStoreScenario.Create(fixture, ct);
        await scenario.Seed(expired, live);

        // Act
        var (history, draftTimeline) = await scenario.ReadMaintenanceAfterWorkerStartup(now, ct);
        var remaining = await scenario.GetDocuments(ct);

        // Assert
        remaining.ShouldHaveSingleItem().Id.ShouldBe(live.Id);
        history.Schedules.Count.ShouldBe(3);
        history.Schedules.All(schedule => schedule.IsPaused == false).ShouldBeTrue();
        draftTimeline.Runs.ShouldBe(history.DraftRuns);
        draftTimeline.Next.ShouldBeNull();
        var schedule = draftTimeline.Schedule.ShouldNotBeNull();
        schedule.ShouldBe(history.Schedules[0]);
        schedule.Job.ShouldBe(DocumentMaintenanceJob.Drafts);
        schedule.FixedDelay.ShouldBe(TimeSpan.FromDays(1));
        schedule.ActiveRun.ShouldBeNull();
        schedule.IsDue.ShouldBeFalse();
        var lastRun = schedule.LatestRun.ShouldNotBeNull();
        lastRun.Status.ShouldBe(DocumentMaintenanceExecutionStatus.Succeeded);
        lastRun.FinishedAtUtc.HasValue.ShouldBeTrue();
        schedule.NextDueAtUtc.ShouldBe(lastRun.FinishedAtUtc.GetValueOrDefault().AddDays(1));
        draftTimeline.Runs.Select(run => run.PurgedCount).Order().ToArray().ShouldBe([0, 1]);
        var audit = history.AuditRuns.ShouldHaveSingleItem();
        var idempotency = history.IdempotencyRuns.ShouldHaveSingleItem();
        audit.PurgedCount.ShouldBe(0);
        idempotency.PurgedCount.ShouldBe(0);
        foreach (var recorded in draftTimeline.Runs.Append(audit).Append(idempotency))
        {
            recorded.OccurredAtUtc.ShouldBe(now.UtcDateTime);
            recorded.Outcome.ShouldBe("Succeeded");
            recorded.HeldCount.ShouldBe(0);
        }
    }

    [Fact]
    public async Task Failed_execution_rolls_back_draft_purge_and_an_explicit_retry_records_recovery()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var now = new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);
        var expired = DocumentDraftInfrastructureTestData.CreateDraft(now.UtcDateTime.AddDays(-DocumentLimits.DraftRetentionDays - 1));
        await using var scenario = await PostgreSqlDocumentStoreScenario.Create(fixture, ct);
        await scenario.Seed(expired);
        await scenario.SetDraftEvidenceRejected(true, ct);

        // Act
        var (faultedHistory, failure) = await scenario.ReadMaintenanceAfterDraftStartupFailure(now, ct);
        var draftsAfterFailure = await scenario.GetDocuments(ct);
        await scenario.SetDraftEvidenceRejected(false, ct);
        await scenario.RetryFailedDraftMaintenance(ct);
        var (recoveredHistory, recoveredTimeline) = await scenario.ReadMaintenanceAfterWorkerStartup(now.AddMinutes(1), ct);
        var draftsAfterRestart = await scenario.GetDocuments(ct);

        // Assert
        faultedHistory.Schedules[0].LatestRun.ShouldNotBeNull().Status.ShouldBe(DocumentMaintenanceExecutionStatus.Failed);
        faultedHistory.DraftRuns.ShouldBeEmpty();
        faultedHistory.AuditRuns.ShouldHaveSingleItem().Outcome.ShouldBe("Succeeded");
        faultedHistory.IdempotencyRuns.ShouldHaveSingleItem().Outcome.ShouldBe("Succeeded");
        draftsAfterFailure.ShouldHaveSingleItem().Id.ShouldBe(expired.Id);
        failure.Exception.ShouldBeNull();
        failure.Message.ShouldContain("DbUpdateException", StringComparison.Ordinal);
        failure.Message.ShouldNotContain("RejectDraftMaintenanceEvidence", StringComparison.Ordinal);
        failure.Message.ShouldNotContain(expired.Id.ToString("D"), StringComparison.Ordinal);

        draftsAfterRestart.ShouldBeEmpty();
        recoveredHistory.DraftRuns.ShouldBe(recoveredTimeline.Runs);
        recoveredTimeline.Next.ShouldBeNull();
        recoveredTimeline.Runs.Select(run => run.PurgedCount).Order().ToArray().ShouldBe([0, 1]);
        foreach (var batch in recoveredTimeline.Runs)
        {
            batch.OccurredAtUtc.ShouldBe(now.UtcDateTime.AddMinutes(1));
            batch.Outcome.ShouldBe("Succeeded");
        }
    }

    [Fact]
    public async Task Timeline_reaches_older_batches_with_equal_timestamps_and_keeps_jobs_separate()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var time = DateTime.UnixEpoch.AddYears(56);
        await using var scenario = await PostgreSqlDocumentStoreScenario.Create(fixture, ct);
        await scenario.MigrateTo(null, ct);
        var drafts = Enumerable.Range(1, 24)
            .Select(index => DocumentRetentionPurgeRecord.Create(
                DocumentRetentionPurgeKind.Drafts, index, 1, DocumentRetentionPurgeOutcome.Succeeded, time).Value)
            .ToArray();
        var idempotency = DocumentRetentionPurgeRecord.Create(
            DocumentRetentionPurgeKind.IdempotencyEntries, 0, 0, DocumentRetentionPurgeOutcome.Refused, time).Value;
        var audit = DocumentAuditPurgeRecord.Create(
            "synthetic-actor", "synthetic-correlation", 31, 2, time.AddHours(-1), DocumentAuditOutcome.Succeeded, time).Value;
        await scenario.SeedMaintenanceEvidence([.. drafts, idempotency], audit, ct);
        var expectedCounts = drafts.OrderByDescending(run => run.Id).Select(run => run.PurgedCount).ToArray();

        // Act
        var first = await scenario.ReadMaintenanceTimeline(DocumentMaintenanceJob.Drafts, 20, null, ct);
        var last = await scenario.ReadMaintenanceTimeline(DocumentMaintenanceJob.Drafts, 20, first.Next, ct);
        var auditTimeline = await scenario.ReadMaintenanceTimeline(DocumentMaintenanceJob.Audit, 20, null, ct);
        var idempotencyTimeline = await scenario.ReadMaintenanceTimeline(DocumentMaintenanceJob.Idempotency, 20, null, ct);
        var allCounts = first.Runs.Concat(last.Runs).Select(run => run.PurgedCount).ToArray();

        // Assert
        first.Runs.Count.ShouldBe(20);
        first.Next.ShouldNotBeNull().OccurredAtUtc.ShouldBe(new DateTimeOffset(time));
        last.Runs.Count.ShouldBe(4);
        last.Next.ShouldBeNull();
        allCounts.ShouldBe(expectedCounts);
        auditTimeline.Runs.ShouldHaveSingleItem().PurgedCount.ShouldBe(31);
        auditTimeline.Next.ShouldBeNull();
        idempotencyTimeline.Runs.ShouldHaveSingleItem().Outcome.ShouldBe("Refused");
        idempotencyTimeline.Next.ShouldBeNull();
    }

    [Fact]
    public async Task Execution_pages_keep_retry_cancellation_and_terminal_evidence_for_the_selected_job()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var drafts = DocumentMaintenanceSchedule.GetJobName(DocumentMaintenanceJob.Drafts);
        var audit = DocumentMaintenanceSchedule.GetJobName(DocumentMaintenanceJob.Audit);
        var failedId = Guid.CreateVersion7();
        var retryId = Guid.CreateVersion7();
        var terminalId = Guid.CreateVersion7();
        var auditId = Guid.CreateVersion7();
        var leaseDuration = TimeSpan.FromMinutes(2);
        await using var scenario = await PostgreSqlDocumentStoreScenario.Create(fixture, ct);
        await scenario.MigrateTo(null, ct);
        await using var pool = scenario.CreateMaintenanceDataSource();
        var scheduler = new PostgreSqlJobScheduler(pool, "document_maintenance");
        var draftsRegistered = await scheduler.Register(drafts, null, null, ct);
        var auditRegistered = await scheduler.Register(audit, null, null, ct);
        draftsRegistered.ShouldBeTrue();
        auditRegistered.ShouldBeTrue();

        var originalRequested = await scheduler.Trigger(drafts, failedId, ct);
        originalRequested.ShouldBeTrue();
        var failed = await scheduler.Acquire(leaseDuration, ct);
        var failureRecorded = await scheduler.Complete(failed.ShouldNotBeNull(), PostgreSqlScheduleRunStatus.Failed, null, ct);
        failureRecorded.ShouldBeTrue();
        var retryRequested = await scheduler.Retry(drafts, failedId, retryId, ct);
        retryRequested.ShouldBeTrue();
        var retry = await scheduler.Acquire(leaseDuration, ct);
        var retryCancelled = await scheduler.Complete(retry.ShouldNotBeNull(), PostgreSqlScheduleRunStatus.Cancelled, null, ct);
        retryCancelled.ShouldBeTrue();

        var terminalRequested = await scheduler.Trigger(drafts, terminalId, ct);
        terminalRequested.ShouldBeTrue();
        var terminal = await scheduler.Acquire(leaseDuration, ct);
        var terminalRecorded = await scheduler.Complete(terminal.ShouldNotBeNull(), PostgreSqlScheduleRunStatus.TerminalFailure, null, ct);
        terminalRecorded.ShouldBeTrue();
        var auditRequested = await scheduler.Trigger(audit, auditId, ct);
        auditRequested.ShouldBeTrue();
        var sibling = await scheduler.Acquire(leaseDuration, ct);
        var siblingCompleted = await scheduler.Complete(sibling.ShouldNotBeNull(), PostgreSqlScheduleRunStatus.Succeeded, null, ct);
        siblingCompleted.ShouldBeTrue();

        // Act
        var latest = await scenario.ReadMaintenanceExecutions(DocumentMaintenanceJob.Drafts, 2, null, ct);
        var cursor = latest.Next.ShouldNotBeNull();
        var earlier = await scenario.ReadMaintenanceExecutions(DocumentMaintenanceJob.Drafts, 2, cursor, ct);
        var siblingHistory = await scenario.ReadMaintenanceExecutions(DocumentMaintenanceJob.Audit, 2, null, ct);

        // Assert
        latest.Executions.Select(run => (run.RunId, run.Status)).ToArray().ShouldBe(
            [(terminalId, DocumentMaintenanceExecutionStatus.TerminalFailure), (retryId, DocumentMaintenanceExecutionStatus.Cancelled)]);
        cursor.RunId.ShouldBe(retryId);
        cursor.StartedAtUtc.ShouldBe(latest.Executions[1].StartedAtUtc);
        var original = earlier.Executions.ShouldHaveSingleItem();
        original.RunId.ShouldBe(failedId);
        original.Status.ShouldBe(DocumentMaintenanceExecutionStatus.RetryScheduled);
        original.RetryAtUtc.ShouldNotBeNull();
        original.FinishedAtUtc.ShouldNotBeNull();
        earlier.Next.ShouldBeNull();
        siblingHistory.Executions.ShouldHaveSingleItem().RunId.ShouldBe(auditId);
        siblingHistory.Next.ShouldBeNull();
    }

    [Fact]
    public async Task A_due_schedule_stops_reporting_due_while_paused_or_held_by_an_active_execution()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var job = DocumentMaintenanceJob.Drafts;
        var jobName = DocumentMaintenanceSchedule.GetJobName(job);
        var inactive = DocumentMaintenanceSchedule.GetJobName(DocumentMaintenanceJob.Audit);
        var dueAt = TimeProvider.System.GetUtcNow().AddHours(-1);
        await using var scenario = await PostgreSqlDocumentStoreScenario.Create(fixture, ct);
        await scenario.MigrateTo(null, ct);
        await using var pool = scenario.CreateMaintenanceDataSource();
        var scheduler = new PostgreSqlJobScheduler(pool, "document_maintenance");
        var draftsRegistered = await scheduler.Register(jobName, dueAt, TimeSpan.FromDays(1), ct);
        var auditRegistered = await scheduler.Register(inactive, null, null, ct);
        draftsRegistered.ShouldBeTrue();
        auditRegistered.ShouldBeTrue();

        // Act
        var waiting = await scenario.ReadMaintenanceHistory(ct);
        var pauseApplied = await scheduler.SetPaused(jobName, true, ct);
        var paused = await scenario.ReadMaintenanceHistory(ct);
        var resumeApplied = await scheduler.SetPaused(jobName, false, ct);
        var acquired = await scheduler.Acquire(TimeSpan.FromMinutes(2), ct);
        var running = await scenario.ReadMaintenanceHistory(ct);

        // Assert
        pauseApplied.ShouldBeTrue();
        resumeApplied.ShouldBeTrue();
        var waitingDrafts = waiting.Schedules.Where(schedule => schedule.Job == job).ShouldHaveSingleItem();
        waitingDrafts.IsPaused.ShouldBe(false);
        waitingDrafts.IsDue.ShouldBeTrue();
        waitingDrafts.ActiveRun.ShouldBeNull();
        waitingDrafts.LatestRun.ShouldBeNull();
        var unscheduledAudit = waiting.Schedules.Where(schedule => schedule.Job == DocumentMaintenanceJob.Audit).ShouldHaveSingleItem();
        unscheduledAudit.IsPaused.ShouldBe(false);
        unscheduledAudit.NextDueAtUtc.ShouldBeNull();
        unscheduledAudit.IsDue.ShouldBeFalse();
        var pausedDrafts = paused.Schedules.Where(schedule => schedule.Job == job).ShouldHaveSingleItem();
        pausedDrafts.IsPaused.ShouldBe(true);
        pausedDrafts.IsDue.ShouldBeFalse();
        pausedDrafts.ActiveRun.ShouldBeNull();
        var runningDrafts = running.Schedules.Where(schedule => schedule.Job == job).ShouldHaveSingleItem();
        runningDrafts.IsPaused.ShouldBe(false);
        runningDrafts.IsDue.ShouldBeFalse();
        var execution = runningDrafts.ActiveRun.ShouldNotBeNull();
        execution.RunId.ShouldBe(acquired.ShouldNotBeNull().RunId);
        execution.Status.ShouldBe(DocumentMaintenanceExecutionStatus.Running);
        runningDrafts.LatestRun.ShouldBe(execution);
    }
}
