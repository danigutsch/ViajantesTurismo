using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.UnitTests.Infrastructure;

namespace ViajantesTurismo.Admin.UnitTests.MigrationService;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.PersistenceCategory)]
public sealed class DatabaseInitializationWorkerTelemetryTests
{
    [Fact]
    public async Task Records_a_successful_initialization_span_without_an_exception_event()
    {
        // Arrange
        List<Activity> stoppedActivities = [];
        using var listener = DatabaseInitializationWorkerTestHelpers.CreateCapturingListener(stoppedActivities);
        var initializationCalled = false;
        using var harness = DatabaseInitializationWorkerHarness.Create(_ =>
        {
            initializationCalled = true;
            return Task.CompletedTask;
        });
        using var worker = harness.CreateWorker();

        // Act
        await DatabaseInitializationWorkerTestHelpers.ExecuteWorker(worker, CancellationToken.None);

        // Assert
        var activity = (stoppedActivities).ShouldHaveSingleItem();
        (activity.OperationName).ShouldBe("DatabaseInitialization");
        (activity.Source.Name).ShouldBe(DatabaseInitializationWorker.ActivitySourceName);
        (activity.Status).ShouldBe(ActivityStatusCode.Ok);
        (activity.StatusDescription).ShouldBeNull();
        (activity.Tags).ShouldContain(static tag => tag.Key == "operation.type" && tag.Value == "database_initialization");
        (activity.Tags).ShouldContain(static tag => tag.Key == "worker.type" && tag.Value == "migration");
        (activity.Events).ShouldNotContain(static activityEvent => activityEvent.Name == "exception");
        (harness.HostLifetime.StopApplicationCalled).ShouldBeTrue();
        (initializationCalled).ShouldBeTrue();
    }

    [Fact]
    public async Task Records_a_failed_initialization_span_with_an_exception_event()
    {
        // Arrange
        List<Activity> stoppedActivities = [];
        using var listener = DatabaseInitializationWorkerTestHelpers.CreateCapturingListener(stoppedActivities);
        var initializationCalled = false;
        using var harness = DatabaseInitializationWorkerHarness.Create(_ =>
        {
            initializationCalled = true;
            throw new InvalidOperationException("boom");
        });
        using var worker = harness.CreateWorker();

        // Act
        var exception = await ((Func<Task>)(() => DatabaseInitializationWorkerTestHelpers.ExecuteWorker(worker, CancellationToken.None))).ShouldThrow<InvalidOperationException>();

        // Assert
        (exception.Message).ShouldBe("boom");
        var activity = (stoppedActivities).ShouldHaveSingleItem();
        (activity.OperationName).ShouldBe("DatabaseInitialization");
        (activity.Source.Name).ShouldBe(DatabaseInitializationWorker.ActivitySourceName);
        (activity.Status).ShouldBe(ActivityStatusCode.Error);
        (activity.StatusDescription).ShouldBe("boom");
        (activity.Tags).ShouldContain(static tag => tag.Key == "operation.type" && tag.Value == "database_initialization");
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
        (initializationCalled).ShouldBeTrue();
    }

    [Fact]
    public async Task Does_not_record_an_error_for_a_cancelled_initialization_span()
    {
        // Arrange
        List<Activity> stoppedActivities = [];
        using var listener = DatabaseInitializationWorkerTestHelpers.CreateCapturingListener(stoppedActivities);
        var initializationCalled = false;
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        using var harness = DatabaseInitializationWorkerHarness.Create(ct =>
        {
            initializationCalled = true;
            ct.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        });
        using var worker = harness.CreateWorker();

        // Act
        await ((Func<Task>)(() => DatabaseInitializationWorkerTestHelpers.ExecuteWorker(worker, cancellation.Token))).ShouldThrow<OperationCanceledException>();

        // Assert
        var activity = (stoppedActivities).ShouldHaveSingleItem();
        (activity.OperationName).ShouldBe("DatabaseInitialization");
        (activity.Source.Name).ShouldBe(DatabaseInitializationWorker.ActivitySourceName);
        (activity.Status).ShouldBe(ActivityStatusCode.Unset);
        (activity.StatusDescription).ShouldBeNull();
        (activity.Tags).ShouldContain(static tag => tag.Key == "operation.type" && tag.Value == "database_initialization");
        (activity.Tags).ShouldContain(static tag => tag.Key == "worker.type" && tag.Value == "migration");
        (activity.Events).ShouldNotContain(static activityEvent => activityEvent.Name == "exception");
        (harness.HostLifetime.StopApplicationCalled).ShouldBeTrue();
        (initializationCalled).ShouldBeTrue();
    }

    [Fact]
    public async Task Records_an_unsignaled_cancellation_as_a_failure()
    {
        // Arrange
        List<Activity> stoppedActivities = [];
        using var listener = DatabaseInitializationWorkerTestHelpers.CreateCapturingListener(stoppedActivities);
        using var harness = DatabaseInitializationWorkerHarness.Create(_ => throw new OperationCanceledException("unexpected"));
        using var worker = harness.CreateWorker();

        // Act
        _ = await ((Func<Task>)(() => DatabaseInitializationWorkerTestHelpers.ExecuteWorker(worker, CancellationToken.None))).ShouldThrow<OperationCanceledException>();

        // Assert
        var activity = stoppedActivities.ShouldHaveSingleItem();
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.StatusDescription.ShouldBe("unexpected");
        activity.Events.ShouldHaveSingleItem(activityEvent => activityEvent.Name == "exception");
    }

    [Fact]
    public async Task Default_worker_runs_development_data_initialization_and_stops_the_host()
    {
        // Arrange
        using var harness = DatabaseInitializationWorkerHarness.CreateWithDefaultInitialization(Environments.Development);
        using var worker = harness.CreateDefaultWorker();

        // Act
        await DatabaseInitializationWorkerTestHelpers.ExecuteWorker(worker, CancellationToken.None);

        // Assert
        await harness.ShouldContainDevelopmentData(TestContext.Current.CancellationToken);
        var probe = harness.StoreProbe.ShouldNotBeNull();
        probe.CatalogResolved.ShouldBeTrue();
        probe.BrandingResolved.ShouldBeTrue();
        probe.ManagementSecurityResolved.ShouldBeTrue();
        (harness.HostLifetime.StopApplicationCalled).ShouldBeTrue();
    }

    [Fact]
    public async Task Default_worker_in_production_skips_development_data_and_stops_the_host()
    {
        // Arrange
        using var harness = DatabaseInitializationWorkerHarness.CreateWithDefaultInitialization(Environments.Production);
        using var worker = harness.CreateDefaultWorker();

        // Act
        await DatabaseInitializationWorkerTestHelpers.ExecuteWorker(worker, CancellationToken.None);

        // Assert
        await harness.ShouldNotContainDevelopmentData(TestContext.Current.CancellationToken);
        harness.HostLifetime.StopApplicationCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task Production_worker_runs_migrations_without_development_data_initialization()
    {
        // Arrange
        var migrationsCalled = false;
        var developmentDataCalled = false;
        using var harness = DatabaseInitializationWorkerHarness.Create(
            Environments.Production,
            _ =>
            {
                migrationsCalled = true;
                return Task.CompletedTask;
            },
            _ =>
            {
                developmentDataCalled = true;
                return Task.CompletedTask;
            });
        using var worker = harness.CreateWorker();

        // Act
        await DatabaseInitializationWorkerTestHelpers.ExecuteWorker(worker, CancellationToken.None);

        // Assert
        migrationsCalled.ShouldBeTrue();
        developmentDataCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task Development_worker_runs_migrations_and_development_data_initialization()
    {
        // Arrange
        var migrationsCalled = false;
        var developmentDataCalled = false;
        using var harness = DatabaseInitializationWorkerHarness.Create(
            Environments.Development,
            _ =>
            {
                migrationsCalled = true;
                return Task.CompletedTask;
            },
            _ =>
            {
                developmentDataCalled = true;
                return Task.CompletedTask;
            });
        using var worker = harness.CreateWorker();

        // Act
        await DatabaseInitializationWorkerTestHelpers.ExecuteWorker(worker, CancellationToken.None);

        // Assert
        migrationsCalled.ShouldBeTrue();
        developmentDataCalled.ShouldBeTrue();
    }

    [Fact]
    public void Uses_the_database_initialization_worker_logger_category()
    {
        // Arrange
        using var harness = DatabaseInitializationWorkerHarness.CreateWithDefaultInitialization(Environments.Development);
        using var loggerProvider = new CapturingLoggerProvider();
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(loggerProvider));

        // Act
        _ = harness.CreateDefaultWorker(loggerFactory);

        // Assert
        loggerProvider.CategoryName.ShouldBe(typeof(DatabaseInitializationWorker).FullName);
    }

}
