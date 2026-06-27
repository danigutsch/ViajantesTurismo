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
        var seeder = new SuccessfulSeeder();
        using var harness = SeederWorkerHarness.Create(seeder);
        using var worker = harness.CreateWorker();

        // Act
        await SeederWorkerTestHelpers.ExecuteWorker(worker, CancellationToken.None);

        // Assert
        var activity = Assert.Single(stoppedActivities);
        Assert.Equal("DatabaseSeeding", activity.OperationName);
        Assert.Equal(SeederWorker.ActivitySourceName, activity.Source.Name);
        Assert.Equal(ActivityStatusCode.Ok, activity.Status);
        Assert.Null(activity.StatusDescription);
        Assert.Contains(activity.Tags, static tag => tag.Key == "operation.type" && tag.Value == "database_seeding");
        Assert.Contains(activity.Tags, static tag => tag.Key == "worker.type" && tag.Value == "migration");
        Assert.DoesNotContain(activity.Events, static activityEvent => activityEvent.Name == "exception");
        Assert.True(harness.HostLifetime.StopApplicationCalled);
        Assert.True(seeder.SeedCalled);
    }

    [Fact]
    public async Task Records_a_failed_seeding_span_with_an_exception_event()
    {
        // Arrange
        List<Activity> stoppedActivities = [];
        using var listener = SeederWorkerTestHelpers.CreateCapturingListener(stoppedActivities);
        var seeder = new FailingSeeder();
        using var harness = SeederWorkerHarness.Create(seeder);
        using var worker = harness.CreateWorker();

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => SeederWorkerTestHelpers.ExecuteWorker(worker, CancellationToken.None));

        // Assert
        Assert.Equal("boom", exception.Message);
        var activity = Assert.Single(stoppedActivities);
        Assert.Equal("DatabaseSeeding", activity.OperationName);
        Assert.Equal(SeederWorker.ActivitySourceName, activity.Source.Name);
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal("boom", activity.StatusDescription);
        Assert.Contains(activity.Tags, static tag => tag.Key == "operation.type" && tag.Value == "database_seeding");
        Assert.Contains(activity.Tags, static tag => tag.Key == "worker.type" && tag.Value == "migration");
        Assert.DoesNotContain(activity.Tags, static tag => tag.Key.StartsWith("exception.", StringComparison.Ordinal));

        var exceptionEvent = Assert.Single(activity.Events, static activityEvent => activityEvent.Name == "exception");
        var exceptionTags = exceptionEvent.Tags;
        Assert.NotNull(exceptionTags);
        Assert.Contains(exceptionTags, static tag =>
            tag.Key == "exception.type" && string.Equals(tag.Value as string, typeof(InvalidOperationException).FullName, StringComparison.Ordinal));
        Assert.Contains(exceptionTags, static tag =>
            tag.Key == "exception.message" && string.Equals(tag.Value as string, "boom", StringComparison.Ordinal));
        Assert.True(harness.HostLifetime.StopApplicationCalled);
        Assert.True(seeder.SeedCalled);
    }

    [Fact]
    public async Task Does_not_record_an_error_for_a_cancelled_seeding_span()
    {
        // Arrange
        List<Activity> stoppedActivities = [];
        using var listener = SeederWorkerTestHelpers.CreateCapturingListener(stoppedActivities);
        var seeder = new CancelledSeeder();
        using var harness = SeederWorkerHarness.Create(seeder);
        using var worker = harness.CreateWorker();

        // Act
        await Assert.ThrowsAsync<OperationCanceledException>(() => SeederWorkerTestHelpers.ExecuteWorker(worker, CancellationToken.None));

        // Assert
        var activity = Assert.Single(stoppedActivities);
        Assert.Equal("DatabaseSeeding", activity.OperationName);
        Assert.Equal(SeederWorker.ActivitySourceName, activity.Source.Name);
        Assert.Equal(ActivityStatusCode.Unset, activity.Status);
        Assert.Null(activity.StatusDescription);
        Assert.Contains(activity.Tags, static tag => tag.Key == "operation.type" && tag.Value == "database_seeding");
        Assert.Contains(activity.Tags, static tag => tag.Key == "worker.type" && tag.Value == "migration");
        Assert.DoesNotContain(activity.Events, static activityEvent => activityEvent.Name == "exception");
        Assert.True(harness.HostLifetime.StopApplicationCalled);
        Assert.True(seeder.SeedCalled);
    }

}
