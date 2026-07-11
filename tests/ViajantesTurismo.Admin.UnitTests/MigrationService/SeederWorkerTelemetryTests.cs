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
        var activity = TestAssert.ExactlyOne(stoppedActivities);
        TestAssert.Equal("DatabaseSeeding", activity.OperationName);
        TestAssert.Equal(SeederWorker.ActivitySourceName, activity.Source.Name);
        TestAssert.Equal(ActivityStatusCode.Ok, activity.Status);
        TestAssert.Null(activity.StatusDescription);
        TestAssert.Contains(activity.Tags, static tag => tag.Key == "operation.type" && tag.Value == "database_seeding");
        TestAssert.Contains(activity.Tags, static tag => tag.Key == "worker.type" && tag.Value == "migration");
        TestAssert.DoesNotContain(activity.Events, static activityEvent => activityEvent.Name == "exception");
        TestAssert.True(harness.HostLifetime.StopApplicationCalled);
        TestAssert.True(seedCalled);
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
        var exception = await TestAssert.Throws<InvalidOperationException>(() => SeederWorkerTestHelpers.ExecuteWorker(worker, CancellationToken.None));

        // Assert
        TestAssert.Equal("boom", exception.Message);
        var activity = TestAssert.ExactlyOne(stoppedActivities);
        TestAssert.Equal("DatabaseSeeding", activity.OperationName);
        TestAssert.Equal(SeederWorker.ActivitySourceName, activity.Source.Name);
        TestAssert.Equal(ActivityStatusCode.Error, activity.Status);
        TestAssert.Equal("boom", activity.StatusDescription);
        TestAssert.Contains(activity.Tags, static tag => tag.Key == "operation.type" && tag.Value == "database_seeding");
        TestAssert.Contains(activity.Tags, static tag => tag.Key == "worker.type" && tag.Value == "migration");
        TestAssert.DoesNotContain(activity.Tags, static tag => tag.Key.StartsWith("exception.", StringComparison.Ordinal));

        var exceptionEvent = TestAssert.ExactlyOne(activity.Events, static activityEvent => activityEvent.Name == "exception");
        var exceptionTags = exceptionEvent.Tags;
        _ = TestAssert.NotNull(exceptionTags);
        TestAssert.Contains(exceptionTags, static tag =>
            tag.Key == "exception.type" && string.Equals(tag.Value as string, typeof(InvalidOperationException).FullName, StringComparison.Ordinal));
        TestAssert.Contains(exceptionTags, static tag =>
            tag.Key == "exception.message" && string.Equals(tag.Value as string, "boom", StringComparison.Ordinal));
        TestAssert.True(harness.HostLifetime.StopApplicationCalled);
        TestAssert.True(seedCalled);
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
        await TestAssert.Throws<OperationCanceledException>(() => SeederWorkerTestHelpers.ExecuteWorker(worker, CancellationToken.None));

        // Assert
        var activity = TestAssert.ExactlyOne(stoppedActivities);
        TestAssert.Equal("DatabaseSeeding", activity.OperationName);
        TestAssert.Equal(SeederWorker.ActivitySourceName, activity.Source.Name);
        TestAssert.Equal(ActivityStatusCode.Unset, activity.Status);
        TestAssert.Null(activity.StatusDescription);
        TestAssert.Contains(activity.Tags, static tag => tag.Key == "operation.type" && tag.Value == "database_seeding");
        TestAssert.Contains(activity.Tags, static tag => tag.Key == "worker.type" && tag.Value == "migration");
        TestAssert.DoesNotContain(activity.Events, static activityEvent => activityEvent.Name == "exception");
        TestAssert.True(harness.HostLifetime.StopApplicationCalled);
        TestAssert.True(seedCalled);
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
        TestAssert.True(harness.HostLifetime.StopApplicationCalled);
    }

}
