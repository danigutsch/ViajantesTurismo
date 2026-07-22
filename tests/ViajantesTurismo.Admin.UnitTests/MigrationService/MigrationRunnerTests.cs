using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace ViajantesTurismo.Admin.UnitTests.MigrationService;

public sealed class MigrationRunnerTests
{
    [Fact]
    public async Task Records_a_successful_seeding_span_without_an_exception_event()
    {
        // Arrange
        List<Activity> stoppedActivities = [];
        var seedCalled = false;
        using var harness = MigrationRunnerHarness.Create(_ =>
        {
            seedCalled = true;
            return Task.CompletedTask;
        });
        using var listener = MigrationRunnerTestHelpers.CreateCapturingListener(harness.ActivitySource, stoppedActivities);
        var runner = harness.CreateRunner();

        // Act
        await runner.Run(CancellationToken.None);

        // Assert
        var activity = (stoppedActivities).ShouldHaveSingleItem();
        (activity.OperationName).ShouldBe("DatabaseSeeding");
        (activity.Source.Name).ShouldBe("ViajantesTurismo.MigrationService.SeederWorker");
        (activity.Status).ShouldBe(ActivityStatusCode.Ok);
        (activity.StatusDescription).ShouldBeNull();
        (activity.Tags).ShouldContain(static tag => tag.Key == "operation.type" && tag.Value == "database_seeding");
        (activity.Tags).ShouldContain(static tag => tag.Key == "worker.type" && tag.Value == "migration");
        (activity.Events).ShouldNotContain(static activityEvent => activityEvent.Name == "exception");
        (seedCalled).ShouldBeTrue();
    }

    [Fact]
    public async Task Records_a_failed_seeding_span_with_an_exception_event()
    {
        // Arrange
        List<Activity> stoppedActivities = [];
        var seedCalled = false;
        using var harness = MigrationRunnerHarness.Create(_ =>
        {
            seedCalled = true;
            throw new InvalidOperationException("boom");
        });
        using var listener = MigrationRunnerTestHelpers.CreateCapturingListener(harness.ActivitySource, stoppedActivities);
        var runner = harness.CreateRunner();

        // Act
        var exception = await ((Func<Task>)(() => runner.Run(CancellationToken.None))).ShouldThrow<InvalidOperationException>();

        // Assert
        (exception.Message).ShouldBe("boom");
        var activity = (stoppedActivities).ShouldHaveSingleItem();
        (activity.OperationName).ShouldBe("DatabaseSeeding");
        (activity.Source.Name).ShouldBe("ViajantesTurismo.MigrationService.SeederWorker");
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
        (seedCalled).ShouldBeTrue();
    }

    [Fact]
    public async Task Does_not_record_an_error_for_a_cancelled_seeding_span()
    {
        // Arrange
        List<Activity> stoppedActivities = [];
        var seedCalled = false;
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        using var harness = MigrationRunnerHarness.Create(ct =>
        {
            seedCalled = true;
            ct.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        });
        using var listener = MigrationRunnerTestHelpers.CreateCapturingListener(harness.ActivitySource, stoppedActivities);
        var runner = harness.CreateRunner();

        // Act
        await ((Func<Task>)(() => runner.Run(cancellation.Token))).ShouldThrow<OperationCanceledException>();

        // Assert
        var activity = (stoppedActivities).ShouldHaveSingleItem();
        (activity.OperationName).ShouldBe("DatabaseSeeding");
        (activity.Source.Name).ShouldBe("ViajantesTurismo.MigrationService.SeederWorker");
        (activity.Status).ShouldBe(ActivityStatusCode.Unset);
        (activity.StatusDescription).ShouldBeNull();
        (activity.Tags).ShouldContain(static tag => tag.Key == "operation.type" && tag.Value == "database_seeding");
        (activity.Tags).ShouldContain(static tag => tag.Key == "worker.type" && tag.Value == "migration");
        (activity.Events).ShouldNotContain(static activityEvent => activityEvent.Name == "exception");
        (seedCalled).ShouldBeTrue();
    }

    [Fact]
    public async Task Records_an_unsignaled_cancellation_as_a_failure()
    {
        // Arrange
        List<Activity> stoppedActivities = [];
        using var harness = MigrationRunnerHarness.Create(_ => throw new OperationCanceledException("unexpected"));
        using var listener = MigrationRunnerTestHelpers.CreateCapturingListener(harness.ActivitySource, stoppedActivities);
        var runner = harness.CreateRunner();

        // Act
        _ = await ((Func<Task>)(() => runner.Run(CancellationToken.None))).ShouldThrow<OperationCanceledException>();

        // Assert
        var activity = stoppedActivities.ShouldHaveSingleItem();
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.StatusDescription.ShouldBe("unexpected");
        activity.Events.ShouldHaveSingleItem(activityEvent => activityEvent.Name == "exception");
    }

    [Fact]
    public async Task Default_runner_runs_database_initialization()
    {
        // Arrange
        using var harness = MigrationRunnerHarness.CreateWithDefaultInitialization();
        var runner = harness.CreateDefaultRunner();

        // Act
        await runner.Run(CancellationToken.None);

        // Assert
        await harness.ShouldContainSeedData(TestContext.Current.CancellationToken);
        var probe = harness.StoreProbe.ShouldNotBeNull();
        probe.CatalogResolved.ShouldBeTrue();
        probe.BrandingResolved.ShouldBeTrue();
        probe.ManagementSecurityResolved.ShouldBeTrue();
    }

    [Fact]
    public void Preserves_the_legacy_logger_category()
    {
        // Arrange
        using var harness = MigrationRunnerHarness.CreateWithDefaultInitialization();
        using var loggerProvider = new CapturingLoggerProvider();
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(loggerProvider));

        // Act
        _ = harness.CreateDefaultRunner(loggerFactory);

        // Assert
        loggerProvider.CategoryName.ShouldBe("ViajantesTurismo.MigrationService.SeederWorker");
    }

}
