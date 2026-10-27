using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using PandaBear.Scheduling;

namespace ViajantesTurismo.Catalog.UnitTests;

/// <summary>Polling background service whose batch signals start and then blocks until cancelled.</summary>
internal sealed class PollingBackgroundServiceTestHarness
    : PollingBackgroundService
{
    private readonly TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Creates the harness with a one-minute interval and a null logger.</summary>
    public PollingBackgroundServiceTestHarness()
        : base(NullLogger.Instance, "privacy-test-poller", TimeSpan.FromMinutes(1))
    {
    }

    /// <summary>Gets a task that completes when the first batch has started.</summary>
    public Task Started => started.Task;

    /// <summary>Creates and registers a listener that collects stopped scheduling activities that are children of the given parent context.</summary>
    public static ActivityListener CreateActivityListener(ConcurrentQueue<Activity> stoppedActivities, ActivityContext parentContext)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = static source => string.Equals(source.Name, SchedulingTelemetry.Name, StringComparison.Ordinal),
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                if (activity.TraceId == parentContext.TraceId && activity.ParentSpanId == parentContext.SpanId)
                {
                    stoppedActivities.Enqueue(activity);
                }
            },
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    protected override async ValueTask<int> ExecuteBatch(CancellationToken ct)
    {
        started.TrySetResult();
        await Task.Delay(Timeout.InfiniteTimeSpan, ct);
        return 0;
    }
}
