using System.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace ViajantesTurismo.Management.WebTests.Reproduction;

/// <summary>Observes the attempted trace ID on the server request without retaining HTTP data.</summary>
internal sealed class DocumentProxyTraceObservation(ActivityTraceId expected)
{
    private int observed;

    /// <summary>Gets the trace ID when an incoming server activity matched the attempt.</summary>
    internal ActivityTraceId? ObservedTrace => Volatile.Read(ref observed) == 1 ? expected : null;

    /// <summary>Subscribes to the hosting activity for this attempt. The caller owns the listener.</summary>
    internal ActivityListener Listen(IHost host)
    {
        var hosting = host.Services.GetRequiredService<ActivitySource>();
        var listener = new ActivityListener
        {
            ShouldListenTo = source => ReferenceEquals(source, hosting),
            Sample = (ref options) =>
                options.Parent.TraceId == expected ? ActivitySamplingResult.PropagationData : ActivitySamplingResult.None,
            SampleUsingParentId = (ref options) =>
                ActivityContext.TryParse(options.Parent, null, out var parent) && parent.TraceId == expected
                    ? ActivitySamplingResult.PropagationData
                    : ActivitySamplingResult.None,
            ActivityStarted = activity =>
            {
                if (activity.Kind == ActivityKind.Server && activity.TraceId == expected)
                {
                    Interlocked.Exchange(ref observed, 1);
                }
            }
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }
}
