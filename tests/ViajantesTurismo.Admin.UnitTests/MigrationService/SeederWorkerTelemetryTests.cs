using System.Diagnostics;
using System.Reflection;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.MigrationService;

namespace ViajantesTurismo.Admin.UnitTests.MigrationService;

public sealed class SeederWorkerTelemetryTests
{
    [Fact]
    public async Task Records_A_Successful_Seeding_Span_Without_An_Exception_Event()
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
    public async Task Records_A_Failed_Seeding_Span_With_An_Exception_Event()
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
    public async Task Does_Not_Record_An_Error_For_A_Cancelled_Seeding_Span()
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

file static class SeederWorkerTestHelpers
{
    public static async Task ExecuteWorker(SeederWorker worker, CancellationToken ct)
    {
        var executeAsync = typeof(SeederWorker).GetMethod(
            "ExecuteAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(executeAsync);
        var executionTask = (Task?)executeAsync.Invoke(worker, [ct]);
        Assert.NotNull(executionTask);
        await executionTask;
    }

    public static ActivityListener CreateCapturingListener(List<Activity> stoppedActivities)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = static source => source.Name == SeederWorker.ActivitySourceName,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            SampleUsingParentId = static (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = stoppedActivities.Add,
        };

        ActivitySource.AddActivityListener(listener);
        return listener;
    }
}

file sealed class SuccessfulSeeder : ISeeder
{
    public bool SeedCalled { get; private set; }

    public Task Seed(CancellationToken ct)
    {
        SeedCalled = true;
        return Task.CompletedTask;
    }

    public Task ClearDatabase(CancellationToken ct) => Task.CompletedTask;
}

file sealed class FailingSeeder : ISeeder
{
    public bool SeedCalled { get; private set; }

    public Task Seed(CancellationToken ct)
    {
        SeedCalled = true;
        return Task.FromException(new InvalidOperationException("boom"));
    }

    public Task ClearDatabase(CancellationToken ct) => Task.CompletedTask;
}

file sealed class CancelledSeeder : ISeeder
{
    public bool SeedCalled { get; private set; }

    public Task Seed(CancellationToken ct)
    {
        SeedCalled = true;
        return Task.FromException(new OperationCanceledException("cancelled"));
    }

    public Task ClearDatabase(CancellationToken ct) => Task.CompletedTask;
}
