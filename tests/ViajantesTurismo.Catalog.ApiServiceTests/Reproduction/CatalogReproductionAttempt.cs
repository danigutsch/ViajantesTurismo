using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.TestHost;
using SharedKernel.Testing.Snapshots;
using ViajantesTurismo.Catalog.Api;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Reproduction;

/// <summary>Correlates an independent Catalog attempt with the trace observed by its test host.</summary>
/// <param name="specification">The approved manifest vocabulary for the attempt.</param>
/// <param name="expectedTrace">The trace to observe on the real server activity.</param>
/// <param name="traceParent">The W3C trace context to propagate.</param>
internal sealed class CatalogReproductionAttempt(
    ReproductionSpecification specification,
    ActivityTraceId expectedTrace,
    string? traceParent) : IStartupFilter
{
    private readonly Guid attemptId = Guid.NewGuid();
    private readonly DateTimeOffset startedUtc = TimeProvider.System.GetUtcNow();
    private readonly long started = Stopwatch.GetTimestamp();
    private int observedRequests;

    /// <summary>Gets the number of requests observed under this attempt's trace.</summary>
    internal int ObservedRequests => Volatile.Read(ref observedRequests);

    /// <summary>Configures the host to observe incoming request trace identities.</summary>
    internal WebApplicationFactory<CatalogApiHostEntryPoint> Observe(WebApplicationFactory<CatalogApiHostEntryPoint> factory) =>
        factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services => services.AddSingleton<IStartupFilter>(this)));

    /// <summary>Propagates the attempt's W3C trace context on the test client.</summary>
    internal void PropagateTrace(HttpClient client)
    {
        if (!client.DefaultRequestHeaders.TryAddWithoutValidation("traceparent", traceParent))
        {
            throw new InvalidOperationException("The test client could not carry the attempt trace context.");
        }
    }

    /// <summary>Captures the approved manifest values and the trace when observed by the server.</summary>
    internal ReproductionManifest Capture() => new(
        specification.Revision, specification.Scenario, attemptId, ObservedRequests == 0 ? [] : [expectedTrace],
        specification.AllowedOutcomes.Single(), startedUtc, checked((int)Stopwatch.GetElapsedTime(started).TotalMilliseconds));

    /// <inheritdoc />
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => application =>
    {
        application.Use(nextRequest => async context =>
        {
            var requestTrace = context.Features.Get<IHttpActivityFeature>()?.Activity.TraceId;
            if (requestTrace == expectedTrace)
            {
                Interlocked.Increment(ref observedRequests);
            }

            await nextRequest(context);
        });
        next(application);
    };
}
