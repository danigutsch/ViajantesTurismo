using System.Collections.Concurrent;
using System.Diagnostics;

namespace ViajantesTurismo.Admin.IntegrationTests.Observability;

public sealed class RequestTraceCorrelationTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Can_Export_A_Correlated_Trace_For_A_Real_Tour_Request()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);

        using var root = new Activity("integration-test-root");
        root.Start();
        var exportedActivities = new ConcurrentQueue<Activity>();
        using var listener = CreateCapturingListener(exportedActivities, root.TraceId);
        ActivitySource.AddActivityListener(listener);

        // Act
        var response = await Client.GetAsync(new Uri($"/tours/{tour.Id}", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var capturedActivities = exportedActivities.ToArray();

        var clientActivity = Assert.Single(capturedActivities, activity =>
            activity.Kind == ActivityKind.Client
            && string.Equals(activity.Source.Name, "System.Net.Http", StringComparison.Ordinal));

        Assert.Equal(root.SpanId, clientActivity.ParentSpanId);

        var serverActivity = Assert.Single(capturedActivities, activity =>
            activity.Kind == ActivityKind.Server
            && string.Equals(activity.Source.Name, "Microsoft.AspNetCore", StringComparison.Ordinal));

        Assert.Equal(clientActivity.SpanId, serverActivity.ParentSpanId);

        Assert.Contains(capturedActivities, activity =>
            activity.Kind == ActivityKind.Client
            && activity.Tags.Any(tag =>
                string.Equals(tag.Key, "db.system.name", StringComparison.Ordinal)
                && string.Equals(tag.Value, "postgresql", StringComparison.Ordinal)));
    }

    private static ActivityListener CreateCapturingListener(
        ConcurrentQueue<Activity> exportedActivities,
        ActivityTraceId traceId)
    {
        return new ActivityListener
        {
            ShouldListenTo = static _ => true,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            SampleUsingParentId = static (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                if (activity.TraceId == traceId)
                {
                    exportedActivities.Enqueue(activity);
                }
            }
        };
    }
}
