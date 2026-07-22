using ViajantesTurismo.MigrationService;

namespace ViajantesTurismo.Admin.UnitTests.MigrationService;

public sealed class MigrationProcessTests
{
    [Fact]
    public async Task Returns_zero_after_successful_run_and_cleanup()
    {
        // Arrange
        using var host = new MigrationProcessTestHost(_ => Task.CompletedTask);

        // Act
        var exitCode = await MigrationProcess.Run(() => host);

        // Assert
        exitCode.ShouldBe(0);
        host.StartCalled.ShouldBeTrue();
        host.SeedCalled.ShouldBeTrue();
        host.SeedToken.ShouldBe(host.ApplicationStopping);
        host.StopCalled.ShouldBeTrue();
        host.DisposeCalled.ShouldBeTrue();
        host.LifecycleEvents.ShouldBe(["Start", "Seed", "Stop", "Dispose"]);
        host.StartToken.IsCancellationRequested.ShouldBeFalse();
        host.StopToken.ShouldBe(CancellationToken.None);
    }

    [Fact]
    public async Task Returns_one_and_cleans_up_after_runner_failure()
    {
        // Arrange
        var failure = new InvalidOperationException("boom");
        using var host = new MigrationProcessTestHost(_ => throw failure);
        Exception? reportedFailure = null;

        // Act
        var exitCode = await MigrationProcess.Run(() => host, exception => reportedFailure = exception);

        // Assert
        exitCode.ShouldBe(1);
        reportedFailure.ShouldBeSameAs(failure);
        host.StartCalled.ShouldBeTrue();
        host.SeedCalled.ShouldBeTrue();
        host.StopCalled.ShouldBeTrue();
        host.DisposeCalled.ShouldBeTrue();
        host.LifecycleEvents.ShouldBe(["Start", "Seed", "Stop", "Dispose"]);
        host.StopToken.ShouldBe(CancellationToken.None);
    }

    [Fact]
    public async Task Returns_one_and_cleans_up_after_cancellation()
    {
        // Arrange
        using var host = new MigrationProcessTestHost(
            ct =>
            {
                ct.ThrowIfCancellationRequested();
                return Task.CompletedTask;
            },
            cancelDuringSeed: true);

        // Act
        var exitCode = await MigrationProcess.Run(() => host);

        // Assert
        exitCode.ShouldBe(1);
        host.SeedToken.IsCancellationRequested.ShouldBeTrue();
        host.StopCalled.ShouldBeTrue();
        host.DisposeCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task Returns_one_and_disposes_after_startup_failure()
    {
        // Arrange
        using var host = new MigrationProcessTestHost(
            _ => Task.CompletedTask,
            startFailure: new InvalidOperationException("start failed"));

        // Act
        var exitCode = await MigrationProcess.Run(() => host);

        // Assert
        exitCode.ShouldBe(1);
        host.StartCalled.ShouldBeTrue();
        host.SeedCalled.ShouldBeFalse();
        host.StopCalled.ShouldBeFalse();
        host.DisposeCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task Returns_one_and_disposes_after_shutdown_failure()
    {
        // Arrange
        using var host = new MigrationProcessTestHost(
            _ => Task.CompletedTask,
            stopFailure: new InvalidOperationException("stop failed"));

        // Act
        var exitCode = await MigrationProcess.Run(() => host);

        // Assert
        exitCode.ShouldBe(1);
        host.StopCalled.ShouldBeTrue();
        host.DisposeCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task Returns_one_after_disposal_failure()
    {
        // Arrange
        using var host = new MigrationProcessTestHost(
            _ => Task.CompletedTask,
            disposeFailure: new InvalidOperationException("dispose failed"));

        // Act
        var exitCode = await MigrationProcess.Run(() => host);

        // Assert
        exitCode.ShouldBe(1);
        host.StopCalled.ShouldBeTrue();
        host.DisposeCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task Reports_runner_and_cleanup_failures_without_masking_the_primary_failure()
    {
        // Arrange
        var runnerFailure = new InvalidOperationException("runner failed");
        var stopFailure = new InvalidOperationException("stop failed");
        var disposeFailure = new InvalidOperationException("dispose failed");
        using var host = new MigrationProcessTestHost(
            _ => throw runnerFailure,
            stopFailure: stopFailure,
            disposeFailure: disposeFailure);
        Exception? reportedFailure = null;

        // Act
        var exitCode = await MigrationProcess.Run(() => host, exception => reportedFailure = exception);

        // Assert
        exitCode.ShouldBe(1);
        var aggregateFailure = reportedFailure.ShouldBeOfType<AggregateException>();
        aggregateFailure.InnerExceptions.ShouldBe([runnerFailure, stopFailure, disposeFailure]);
        host.LifecycleEvents.ShouldBe(["Start", "Seed", "Stop", "Dispose"]);
    }
}
