using System.Diagnostics;
using ViajantesTurismo.MigrationService;

namespace ViajantesTurismo.Admin.UnitTests.MigrationService;

public sealed class SeederWorkerTelemetryTests
{
    [Fact]
    public async Task Records_a_successful_seeding_span_without_an_exception_event()
    {
        // Arrange
        List<Activity> stoppedActivities = [];
        using var listener = SeederWorkerTestHelpers.CreateCapturingListener(stoppedActivities);
        var seedCalled = false;
        using var harness = SeederWorkerHarness.Create(_ =>
        {
            seedCalled = true;
            return Task.CompletedTask;
        });
        using var worker = harness.CreateWorker();

        // Act
        await SeederWorkerTestHelpers.ExecuteWorker(worker, CancellationToken.None);

        // Assert
        var activity = (stoppedActivities).ShouldHaveSingleItem();
        (activity.OperationName).ShouldBe("DatabaseSeeding");
        (activity.Source.Name).ShouldBe(SeederWorker.ActivitySourceName);
        (activity.Status).ShouldBe(ActivityStatusCode.Ok);
        (activity.StatusDescription).ShouldBeNull();
        (activity.Tags).ShouldContain(static tag => tag.Key == "operation.type" && tag.Value == "database_seeding");
        (activity.Tags).ShouldContain(static tag => tag.Key == "worker.type" && tag.Value == "migration");
        (activity.Events).ShouldNotContain(static activityEvent => activityEvent.Name == "exception");
        (harness.HostLifetime.StopApplicationCalled).ShouldBeTrue();
        (seedCalled).ShouldBeTrue();
    }

    [Fact]
    public async Task Records_a_failed_seeding_span_with_an_exception_event()
    {
        // Arrange
        List<Activity> stoppedActivities = [];
        using var listener = SeederWorkerTestHelpers.CreateCapturingListener(stoppedActivities);
        var seedCalled = false;
        using var harness = SeederWorkerHarness.Create(_ =>
        {
            seedCalled = true;
            throw new InvalidOperationException("boom");
        });
        using var worker = harness.CreateWorker();

        // Act
        var exception = await ((Func<Task>)(() => SeederWorkerTestHelpers.ExecuteWorker(worker, CancellationToken.None))).ShouldThrow<InvalidOperationException>();

        // Assert
        (exception.Message).ShouldBe("boom");
        var activity = (stoppedActivities).ShouldHaveSingleItem();
        (activity.OperationName).ShouldBe("DatabaseSeeding");
        (activity.Source.Name).ShouldBe(SeederWorker.ActivitySourceName);
        (activity.Status).ShouldBe(ActivityStatusCode.Error);
        (activity.StatusDescription).ShouldBe("boom");
        (activity.Tags).ShouldContain(static tag => tag.Key == "operation.type" && tag.Value == "database_seeding");
        (activity.Tags).ShouldContain(static tag => tag.Key == "worker.type" && tag.Value == "migration");
        (activity.Tags).ShouldNotContain(static tag => tag.Key.StartsWith("exception.", StringComparison.Ordinal));

        var exceptionEvent = (activity.Events).ShouldHaveSingleItem(static activityEvent => activityEvent.Name == "exception");
        var exceptionTags = exceptionEvent.Tags;
        _ = (exceptionTags).ShouldNotBeNull();
        (exceptionTags).ShouldContain(static tag =>
            tag.Key == "exception.type" && string.Equals(tag.Value as string, typeof(InvalidOperationException).FullName, StringComparison.Ordinal));
        (exceptionTags).ShouldContain(static tag =>
            tag.Key == "exception.message" && string.Equals(tag.Value as string, "boom", StringComparison.Ordinal));
        (harness.HostLifetime.StopApplicationCalled).ShouldBeTrue();
        (seedCalled).ShouldBeTrue();
    }

    [Fact]
    public async Task Does_not_record_an_error_for_a_cancelled_seeding_span()
    {
        // Arrange
        List<Activity> stoppedActivities = [];
        using var listener = SeederWorkerTestHelpers.CreateCapturingListener(stoppedActivities);
        var seedCalled = false;
        using var harness = SeederWorkerHarness.Create(_ =>
        {
            seedCalled = true;
            throw new OperationCanceledException();
        });
        using var worker = harness.CreateWorker();

        // Act
        await ((Func<Task>)(() => SeederWorkerTestHelpers.ExecuteWorker(worker, CancellationToken.None))).ShouldThrow<OperationCanceledException>();

        // Assert
        var activity = (stoppedActivities).ShouldHaveSingleItem();
        (activity.OperationName).ShouldBe("DatabaseSeeding");
        (activity.Source.Name).ShouldBe(SeederWorker.ActivitySourceName);
        (activity.Status).ShouldBe(ActivityStatusCode.Unset);
        (activity.StatusDescription).ShouldBeNull();
        (activity.Tags).ShouldContain(static tag => tag.Key == "operation.type" && tag.Value == "database_seeding");
        (activity.Tags).ShouldContain(static tag => tag.Key == "worker.type" && tag.Value == "migration");
        (activity.Events).ShouldNotContain(static activityEvent => activityEvent.Name == "exception");
        (harness.HostLifetime.StopApplicationCalled).ShouldBeTrue();
        (seedCalled).ShouldBeTrue();
    }

    [Fact]
    public async Task Default_worker_runs_database_initialization_and_stops_the_host()
    {
        // Arrange
        using var harness = SeederWorkerHarness.CreateWithDefaultInitialization();
        using var worker = harness.CreateDefaultWorker();

        // Act
        await SeederWorkerTestHelpers.ExecuteWorker(worker, CancellationToken.None);

        // Assert
        await harness.ShouldContainSeedData(TestContext.Current.CancellationToken);
        (harness.HostLifetime.StopApplicationCalled).ShouldBeTrue();
    }

}
