using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
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
        using var listener = CreateCapturingListener(stoppedActivities);
        var seeder = new SuccessfulSeeder();
        var hostLifetime = new TestHostApplicationLifetime();
        using var serviceProvider = CreateServiceProvider(seeder);
        using var worker = new SeederWorker(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<SeederWorker>.Instance,
            hostLifetime);

        // Act
        await InvokeExecuteAsync(worker, CancellationToken.None);

        // Assert
        var activity = Assert.Single(stoppedActivities);
        Assert.Equal("DatabaseSeeding", activity.OperationName);
        Assert.Equal(SeederWorker.ActivitySourceName, activity.Source.Name);
        Assert.Equal(ActivityStatusCode.Ok, activity.Status);
        Assert.Null(activity.StatusDescription);
        Assert.Contains(activity.Tags, static tag => tag.Key == "operation.type" && tag.Value == "database_seeding");
        Assert.Contains(activity.Tags, static tag => tag.Key == "worker.type" && tag.Value == "migration");
        Assert.DoesNotContain(activity.Events, static activityEvent => activityEvent.Name == "exception");
        Assert.True(hostLifetime.StopApplicationCalled);
        Assert.True(seeder.SeedCalled);
    }

    [Fact]
    public async Task Records_A_Failed_Seeding_Span_With_An_Exception_Event()
    {
        // Arrange
        List<Activity> stoppedActivities = [];
        using var listener = CreateCapturingListener(stoppedActivities);
        var seeder = new FailingSeeder();
        var hostLifetime = new TestHostApplicationLifetime();
        using var serviceProvider = CreateServiceProvider(seeder);
        using var worker = new SeederWorker(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<SeederWorker>.Instance,
            hostLifetime);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => InvokeExecuteAsync(worker, CancellationToken.None));

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
        Assert.True(hostLifetime.StopApplicationCalled);
        Assert.True(seeder.SeedCalled);
    }

    private static ServiceProvider CreateServiceProvider(ISeeder seeder)
    {
        var services = new ServiceCollection();
        services.AddSingleton(seeder);
        return services.BuildServiceProvider();
    }

    private static async Task InvokeExecuteAsync(SeederWorker worker, CancellationToken cancellationToken)
    {
        var executeAsync = typeof(SeederWorker).GetMethod(
            "ExecuteAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(executeAsync);
        var executionTask = (Task?)executeAsync.Invoke(worker, [cancellationToken]);
        Assert.NotNull(executionTask);
        await executionTask;
    }

    private static ActivityListener CreateCapturingListener(List<Activity> stoppedActivities)
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

    private sealed class SuccessfulSeeder : ISeeder
    {
        public bool SeedCalled { get; private set; }

        public Task Seed(CancellationToken ct)
        {
            SeedCalled = true;
            return Task.CompletedTask;
        }

        public Task ClearDatabase(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FailingSeeder : ISeeder
    {
        public bool SeedCalled { get; private set; }

        public Task Seed(CancellationToken ct)
        {
            SeedCalled = true;
            return Task.FromException(new InvalidOperationException("boom"));
        }

        public Task ClearDatabase(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class TestHostApplicationLifetime : IHostApplicationLifetime
    {
        public bool StopApplicationCalled { get; private set; }

        public CancellationToken ApplicationStarted => CancellationToken.None;

        public CancellationToken ApplicationStopping => CancellationToken.None;

        public CancellationToken ApplicationStopped => CancellationToken.None;

        public void StopApplication()
        {
            StopApplicationCalled = true;
        }
    }
}
