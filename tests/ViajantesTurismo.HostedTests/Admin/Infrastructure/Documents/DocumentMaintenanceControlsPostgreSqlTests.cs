using Microsoft.EntityFrameworkCore;
using Npgsql;
using SharedKernel.IntegrationTesting;
using SharedKernel.Npgsql;
using SharedKernel.Results;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Application.Documents.Maintenance;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Documents;

[Trait(TestTraitNames.CategoryName, IntegrationTestTraitValues.DatabaseIntegrationCategory)]
[TestScope(TestScope.Integration)]
public sealed class DocumentMaintenanceControlsPostgreSqlTests(PostgreSqlTestServerFixture fixture)
    : IClassFixture<PostgreSqlTestServerFixture>
{
    [Fact]
    public async Task A_failed_audit_write_rolls_back_the_schedule_change()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await PostgreSqlDocumentStoreScenario.Create(fixture, ct);
        await scenario.MigrateTo(null, ct);
        await scenario.RegisterMaintenanceSchedule(DocumentMaintenanceJob.Drafts, ct);
        await scenario.SetMaintenanceControlAuditRejected(reject: true, ct);
        var pause = new ControlDocumentMaintenanceCommand(DocumentMaintenanceJob.Drafts, DocumentMaintenanceControlAction.Pause,
            new CreateDocumentMaintenanceControlDto
            {
                RequestId = Guid.CreateVersion7(),
                Confirmed = true,
                Reason = "Synthetic operator pause."
            },
            "test-scheduler-actor");

        // Act
        Func<Task> apply = () => scenario.ControlMaintenance(pause, ct);
        var error = await apply.ShouldThrow<DbUpdateException>();
        var (rolledBack, rolledBackAudit) = await scenario.ReadMaintenanceControl(DocumentMaintenanceJob.Drafts, ct);
        await scenario.SetMaintenanceControlAuditRejected(reject: false, ct);
        var appliedRetry = await scenario.ControlMaintenance(pause, ct);
        var (committed, committedAudit) = await scenario.ReadMaintenanceControl(DocumentMaintenanceJob.Drafts, ct);

        // Assert
        var postgres = error.InnerException.ShouldBeOfType<PostgresException>();
        postgres.SqlState.ShouldBe(PostgresErrorCodes.CheckViolation);
        postgres.ConstraintName.ShouldBe("reject_test_control");
        rolledBack.IsPaused.ShouldBeFalse();
        rolledBackAudit.ShouldBeEmpty();
        appliedRetry.IsSuccess.ShouldBeTrue();
        committed.IsPaused.ShouldBeTrue();
        var recorded = committedAudit.ShouldHaveSingleItem();
        recorded.RequestId.ShouldBe(pause.Input.RequestId);
        recorded.Action.ShouldBe(pause.Action);
        recorded.ActorId.ShouldBe(pause.ActorId);
        recorded.Reason.ShouldBe(pause.Input.Reason);
    }

    [Fact]
    public async Task An_accepted_request_replays_once_without_overwriting_a_later_control()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await PostgreSqlDocumentStoreScenario.Create(fixture, ct);
        await scenario.MigrateTo(null, ct);
        await scenario.RegisterMaintenanceSchedule(DocumentMaintenanceJob.Drafts, ct);
        var pause = new ControlDocumentMaintenanceCommand(DocumentMaintenanceJob.Drafts, DocumentMaintenanceControlAction.Pause,
            new CreateDocumentMaintenanceControlDto
            {
                RequestId = Guid.CreateVersion7(),
                Confirmed = true,
                Reason = "Synthetic operator pause."
            },
            "test-scheduler-actor");
        var resume = pause with
        {
            Action = DocumentMaintenanceControlAction.Resume,
            Input = pause.Input with
            {
                RequestId = Guid.CreateVersion7(),
                Reason = "Synthetic operator resume."
            }
        };

        // Act
        var competing = await Task.WhenAll(
            scenario.ControlMaintenance(pause, ct), scenario.ControlMaintenance(pause, ct));
        var resumed = await scenario.ControlMaintenance(resume, ct);
        var replay = await scenario.ControlMaintenance(pause, ct);
        var mismatch = await scenario.ControlMaintenance(pause with
        {
            ActorId = "different-scheduler-actor"
        }, ct);
        var (schedule, audit) = await scenario.ReadMaintenanceControl(DocumentMaintenanceJob.Drafts, ct);

        // Assert
        competing.All(result => result.IsSuccess).ShouldBeTrue();
        resumed.IsSuccess.ShouldBeTrue();
        replay.IsSuccess.ShouldBeTrue();
        mismatch.Status.ShouldBe(ResultStatus.Conflict);
        schedule.IsPaused.ShouldBeFalse();
        audit.Length.ShouldBe(2);
        audit.Count(record => record.RequestId == pause.Input.RequestId).ShouldBe(1);
        audit.Count(record => record.RequestId == resume.Input.RequestId).ShouldBe(1);
    }

    [Fact]
    public async Task A_trigger_for_an_unregistered_schedule_can_be_retried_after_registration_and_queues_once()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var job = DocumentMaintenanceJob.Drafts;
        var input = new CreateDocumentMaintenanceControlDto
        {
            RequestId = Guid.CreateVersion7(),
            Confirmed = true,
            Reason = "Synthetic requested maintenance."
        };
        var trigger = new ControlDocumentMaintenanceCommand(job, DocumentMaintenanceControlAction.Trigger, input, "test-scheduler-actor");
        await using var scenario = await PostgreSqlDocumentStoreScenario.Create(fixture, ct);
        await scenario.MigrateTo(null, ct);
        await using var pool = scenario.CreateMaintenanceDataSource();
        var scheduler = new PostgreSqlJobScheduler(pool, "document_maintenance");

        // Act
        var rejected = await scenario.ControlMaintenance(trigger, ct);
        var beforeRegistration = await scheduler.Acquire(TimeSpan.FromMinutes(2), ct);
        await scenario.RegisterMaintenanceSchedule(job, ct);
        var (registered, initialAudit) = await scenario.ReadMaintenanceControl(job, ct);
        var accepted = await scenario.ControlMaintenance(trigger, ct);
        var replayed = await scenario.ControlMaintenance(trigger, ct);
        var acquired = await scheduler.Acquire(TimeSpan.FromMinutes(2), ct);
        var (schedule, audit) = await scenario.ReadMaintenanceControl(job, ct);

        // Assert
        rejected.Status.ShouldBe(ResultStatus.Conflict);
        beforeRegistration.ShouldBeNull();
        initialAudit.ShouldBeEmpty();
        accepted.IsSuccess.ShouldBeTrue();
        replayed.IsSuccess.ShouldBeTrue();
        var run = acquired.ShouldNotBeNull();
        run.RunId.ShouldBe(input.RequestId);
        run.Kind.ShouldBe(PostgreSqlJobRunKind.Manual);
        schedule.NextDueAt.ShouldBe(registered.NextDueAt);
        audit.ShouldHaveSingleItem().RequestId.ShouldBe(input.RequestId);
    }
}
