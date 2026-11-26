using Npgsql;
using SharedKernel.IntegrationTesting;
using SharedKernel.Npgsql;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Infrastructure.Documents;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Documents;

[Trait(TestTraitNames.CategoryName, IntegrationTestTraitValues.DatabaseIntegrationCategory)]
[TestScope(TestScope.Integration)]
public sealed class DurableDocumentMaintenancePostgreSqlTests(PostgreSqlTestServerFixture fixture)
    : IClassFixture<PostgreSqlTestServerFixture>
{
    [Fact]
    public async Task Scheduler_startup_requires_the_governed_schema()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await PostgreSqlDocumentStoreScenario.Create(fixture, ct);
        await scenario.MigrateTo("20261123164511_IndexPaymentAuditsByTargetAndSequence", ct);
        await using var host = scenario.CreateDurableMaintenanceHost();

        // Act
        Func<Task> start = () => host.Start(ct);
        var error = await start.ShouldThrow<PostgresException>();

        // Assert
        error.SqlState.ShouldBe(PostgresErrorCodes.UndefinedTable);
    }

    [Fact]
    public async Task The_three_durable_workers_keep_the_first_completion_based_delay_across_restart()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var now = TimeProvider.System.GetUtcNow();
        var expired = DocumentDraftInfrastructureTestData.CreateDraft(now.UtcDateTime.AddDays(-DocumentLimits.DraftRetentionDays - 1));
        var live = DocumentDraftInfrastructureTestData.CreateDraft(now.UtcDateTime);
        await using var scenario = await PostgreSqlDocumentStoreScenario.Create(fixture, ct);
        await scenario.Seed(expired, live);
        await using var pool = scenario.CreateMaintenanceDataSource();
        var scheduler = new PostgreSqlJobScheduler(pool, DocumentMaintenanceSchedule.DurableSchema);
        var jobs = Enum.GetValues<DocumentMaintenanceJob>();

        // Act
        var completions = new List<(DocumentMaintenanceJob Job, PostgreSqlScheduleRun Run)>();
        await using (var first = scenario.CreateDurableMaintenanceHost())
        {
            await first.Start(ct);
            foreach (var job in jobs)
            {
                var run = await PostgreSqlDocumentStoreScenario.WaitForMaintenanceRun(
                    scheduler, job, PostgreSqlScheduleRunStatus.Succeeded, ct);
                completions.Add((job, run));
            }
        }

        await using (var second = scenario.CreateDurableMaintenanceHost())
        {
            await second.Start(ct);
        }

        // Assert
        var documents = await scenario.GetDocuments(ct);
        documents.ShouldHaveSingleItem().Id.ShouldBe(live.Id);
        foreach (var (job, run) in completions)
        {
            var jobName = DocumentMaintenanceSchedule.GetJobName(job);
            var schedule = await scheduler.ReadJob(jobName, ct);
            var history = await scheduler.ReadHistory(jobName, null, 2, ct);
            schedule.ShouldNotBeNull().NextDueAt.ShouldBe(run.FinishedAt + TimeSpan.FromDays(1));
            history.Runs.ShouldHaveSingleItem().RunId.ShouldBe(run.RunId);
        }
    }

    [Fact]
    public async Task A_held_draft_drain_renews_ownership_and_shutdown_leaves_the_same_run_recoverable()
    {
        // Arrange
        const string draftServiceName = "admin-document-draft-retention";
        var ct = TestContext.Current.CancellationToken;
        var expired = DocumentDraftInfrastructureTestData.CreateDraft(
            TimeProvider.System.GetUtcNow().UtcDateTime.AddDays(-DocumentLimits.DraftRetentionDays - 1));
        await using var scenario = await PostgreSqlDocumentStoreScenario.Create(fixture, ct);
        await scenario.Seed(expired);
        await using var pool = scenario.CreateMaintenanceDataSource();
        var scheduler = new PostgreSqlJobScheduler(pool, DocumentMaintenanceSchedule.DurableSchema);
        var jobName = DocumentMaintenanceSchedule.GetJobName(DocumentMaintenanceJob.Drafts);
        await using var blocker = await pool.OpenConnectionAsync(ct);
        await using var held = await blocker.BeginTransactionAsync(ct);
        await using var holdDrafts = new NpgsqlCommand("""LOCK TABLE public."DocumentDrafts" IN ACCESS EXCLUSIVE MODE""", blocker, held);
        _ = await holdDrafts.ExecuteNonQueryAsync(ct);

        // Act
        PostgreSqlScheduleRun original;
        DateTimeOffset initialDeadline;
        DateTimeOffset renewedDeadline;
        PostgreSqlJobLease? competing;
        double[] activeDraftAges;
        await using (var first = scenario.CreateDurableMaintenanceHost())
        {
            await first.Start(ct);
            original = await PostgreSqlDocumentStoreScenario.WaitForMaintenanceRun(
                scheduler, DocumentMaintenanceJob.Drafts, PostgreSqlScheduleRunStatus.Running, ct);
            initialDeadline = await PostgreSqlDocumentStoreScenario.ReadMaintenanceLeaseDeadline(pool, original.RunId, ct);
            await scenario.WaitForBlockedSession(ct);
            activeDraftAges = DurableDocumentMaintenanceTestHost.ReadBatchAges(draftServiceName);
            renewedDeadline = await PostgreSqlDocumentStoreScenario.WaitForMaintenanceLeaseRenewal(pool, original.RunId, initialDeadline, ct);
            _ = await PostgreSqlDocumentStoreScenario.WaitForMaintenanceRun(
                scheduler, DocumentMaintenanceJob.Audit, PostgreSqlScheduleRunStatus.Succeeded, ct);
            _ = await PostgreSqlDocumentStoreScenario.WaitForMaintenanceRun(
                scheduler, DocumentMaintenanceJob.Idempotency, PostgreSqlScheduleRunStatus.Succeeded, ct);
            competing = await scheduler.Acquire(TimeSpan.FromMinutes(2), ct);
        }

        var shutdownHistory = await scheduler.ReadHistory(jobName, null, 1, ct);
        var stoppedDraftAges = DurableDocumentMaintenanceTestHost.ReadBatchAges(draftServiceName);
        await held.RollbackAsync(ct);
        var expiredLeases = await PostgreSqlDocumentStoreScenario.ExpireMaintenanceLease(pool, original.RunId, ct);
        PostgreSqlScheduleRun recovered;
        double[] idleDraftAges;
        await using (var second = scenario.CreateDurableMaintenanceHost())
        {
            await second.Start(ct);
            recovered = await PostgreSqlDocumentStoreScenario.WaitForMaintenanceRun(
                scheduler, DocumentMaintenanceJob.Drafts, PostgreSqlScheduleRunStatus.Succeeded, ct);
            idleDraftAges = DurableDocumentMaintenanceTestHost.ReadBatchAges(draftServiceName);
        }

        // Assert
        activeDraftAges.ShouldHaveSingleItem().ShouldBeGreaterThan(0d);
        stoppedDraftAges.ShouldBeEmpty();
        idleDraftAges.ShouldHaveSingleItem().ShouldBe(0d);
        renewedDeadline.ShouldBeGreaterThan(initialDeadline);
        competing.ShouldBeNull();
        var interrupted = shutdownHistory.Runs.ShouldHaveSingleItem();
        interrupted.RunId.ShouldBe(original.RunId);
        interrupted.FinishedAt.ShouldBeNull();
        expiredLeases.ShouldBe(1);
        recovered.RunId.ShouldBe(original.RunId);
        var documents = await scenario.GetDocuments(ct);
        documents.ShouldBeEmpty();
    }

    [Fact]
    public async Task An_unrecognized_schedule_is_terminally_refused_without_running_a_maintenance_handler()
    {
        // Arrange
        const string unrecognizedJob = "synthetic-unrecognized-maintenance";
        var ct = TestContext.Current.CancellationToken;
        var requestId = Guid.CreateVersion7();
        await using var scenario = await PostgreSqlDocumentStoreScenario.Create(fixture, ct);
        await scenario.MigrateTo(null, ct);
        await using var pool = scenario.CreateMaintenanceDataSource();
        var scheduler = new PostgreSqlJobScheduler(pool, "document_maintenance");
        foreach (var job in DocumentMaintenanceSchedule.Jobs)
        {
            var registered = await scheduler.Register(DocumentMaintenanceSchedule.GetJobName(job), null, null, ct);
            registered.ShouldBeTrue();
        }

        var unknownRegistered = await scheduler.Register(unrecognizedJob, null, null, ct);
        var requested = await scheduler.Trigger(unrecognizedJob, requestId, ct);
        unknownRegistered.ShouldBeTrue();
        requested.ShouldBeTrue();
        await using var host = scenario.CreateDurableMaintenanceHost();

        // Act
        await host.Start(ct);
        var refused = await PostgreSqlDocumentStoreScenario.WaitForMaintenanceRun(
            scheduler, unrecognizedJob, PostgreSqlScheduleRunStatus.TerminalFailure, ct);
        var available = await scheduler.Acquire(TimeSpan.FromMinutes(2), ct);
        var history = await scenario.ReadMaintenanceHistory(ct);

        // Assert
        refused.RunId.ShouldBe(requestId);
        refused.FinishedAt.ShouldNotBeNull();
        available.ShouldBeNull();
        history.DraftRuns.ShouldBeEmpty();
        history.AuditRuns.ShouldBeEmpty();
        history.IdempotencyRuns.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_superseded_draft_worker_cancels_its_blocked_batch_and_leaves_the_current_lease_finishable()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var job = DocumentMaintenanceJob.Drafts;
        var jobName = DocumentMaintenanceSchedule.GetJobName(job);
        var expired = DocumentDraftInfrastructureTestData.CreateDraft(
            TimeProvider.System.GetUtcNow().UtcDateTime.AddDays(-DocumentLimits.DraftRetentionDays - 1));
        await using var scenario = await PostgreSqlDocumentStoreScenario.Create(fixture, ct);
        await scenario.Seed(expired);
        await using var pool = scenario.CreateMaintenanceDataSource();
        var scheduler = new PostgreSqlJobScheduler(pool, "document_maintenance");
        var auditRegistered = await scheduler.Register(DocumentMaintenanceSchedule.GetJobName(DocumentMaintenanceJob.Audit), null, null, ct);
        var idempotencyRegistered = await scheduler.Register(DocumentMaintenanceSchedule.GetJobName(DocumentMaintenanceJob.Idempotency), null, null, ct);
        auditRegistered.ShouldBeTrue();
        idempotencyRegistered.ShouldBeTrue();
        await using var blocker = await pool.OpenConnectionAsync(ct);
        await using var held = await blocker.BeginTransactionAsync(ct);
        await using var holdDrafts = new NpgsqlCommand("""LOCK TABLE public."DocumentDrafts" IN ACCESS EXCLUSIVE MODE""", blocker, held);
        _ = await holdDrafts.ExecuteNonQueryAsync(ct);
        await using var host = scenario.CreateDurableMaintenanceHost();
        await host.Start(ct);
        var original = await PostgreSqlDocumentStoreScenario.WaitForMaintenanceRun(scheduler, job, PostgreSqlScheduleRunStatus.Running, ct);
        await scenario.WaitForBlockedSession(ct);

        // Act
        var expiredLeases = await PostgreSqlDocumentStoreScenario.ExpireMaintenanceLease(pool, original.RunId, ct);
        var replacement = await scheduler.Acquire(TimeSpan.FromMinutes(2), ct);
        await scenario.WaitForUnblockedSessions(ct);
        await held.RollbackAsync(ct);
        var current = await scheduler.ReadActive(jobName, null, 1, ct);
        var finalized = await scheduler.Complete(replacement.ShouldNotBeNull(), PostgreSqlScheduleRunStatus.Cancelled, null, ct);
        var documents = await scenario.GetDocuments(ct);

        // Assert
        expiredLeases.ShouldBe(1);
        replacement.RunId.ShouldBe(original.RunId);
        var active = current.Runs.ShouldHaveSingleItem();
        active.RunId.ShouldBe(replacement.RunId);
        active.FinishedAt.ShouldBeNull();
        finalized.ShouldBeTrue();
        documents.ShouldHaveSingleItem().Id.ShouldBe(expired.Id);
    }
}
