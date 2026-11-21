using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;

namespace ViajantesTurismo.Admin.ApiServiceTests.Tours;

/// <summary>Observes only the trace identity of the attempt's server activity.</summary>
internal sealed class TourRetirementTraceObservation(ActivityTraceId expected) : IStartupFilter
{
    private int _observed;

    /// <summary>Gets the expected trace ID once the host has received it.</summary>
    internal ActivityTraceId? ObservedTrace => Volatile.Read(ref _observed) == 1 ? expected : null;

    /// <inheritdoc />
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => application =>
    {
        application.Use(nextRequest => async context =>
        {
            var requestTrace = context.Features.Get<IHttpActivityFeature>()?.Activity.TraceId;
            if (requestTrace == expected)
            {
                Interlocked.Exchange(ref _observed, 1);
            }

            await nextRequest(context);
        });
        next(application);
    };
}
