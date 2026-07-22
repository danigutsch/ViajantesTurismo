using System.Diagnostics;
using SharedKernel.Scheduling;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class PollingBackgroundServiceTelemetryTests
{
    [Fact]
    public async Task Cooperative_shutdown_does_not_record_a_polling_error()
    {
        // Arrange
        var stoppedActivities = new List<Activity>();
        using var listener = PollingBackgroundServiceTestHarness.CreateActivityListener(stoppedActivities);
        using var service = new PollingBackgroundServiceTestHarness();
        await service.StartAsync(CancellationToken.None);
        await service.Started.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Act
        await service.StopAsync(TestContext.Current.CancellationToken);

        // Assert
        var activity = stoppedActivities.ShouldHaveSingleItem();
        activity.Status.ShouldBe(ActivityStatusCode.Unset);
        activity.GetTagItem(SchedulingTelemetry.TagOutcome).ShouldBe(SchedulingTelemetry.OutcomeCancelled);
        activity.GetTagItem(SchedulingTelemetry.TagErrorType).ShouldBeNull();
    }
}
