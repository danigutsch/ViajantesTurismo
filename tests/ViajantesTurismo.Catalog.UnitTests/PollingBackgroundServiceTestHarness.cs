using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using SharedKernel.Scheduling;

namespace ViajantesTurismo.Catalog.UnitTests;

internal sealed class PollingBackgroundServiceTestHarness
    : PollingBackgroundService
{
    private readonly TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public PollingBackgroundServiceTestHarness()
        : base(NullLogger.Instance, "privacy-test-poller", TimeSpan.FromMinutes(1))
    {
    }

    public Task Started => started.Task;

    public static ActivityListener CreateActivityListener(ICollection<Activity> stoppedActivities)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = static source => string.Equals(source.Name, SchedulingTelemetry.Name, StringComparison.Ordinal),
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = stoppedActivities.Add,
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    protected override async ValueTask<int> ExecuteBatch(CancellationToken stoppingToken)
    {
        started.TrySetResult();
        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        return 0;
    }
}
