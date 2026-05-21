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

        var capturedActivities = await WaitForCapturedActivities(exportedActivities, TestContext.Current.CancellationToken);

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
            ShouldListenTo = static source =>
                source.Name.StartsWith("System.Net.Http", StringComparison.Ordinal)
                || source.Name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal)
                || source.Name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
                || source.Name.StartsWith("Npgsql", StringComparison.Ordinal),
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

    private static async Task<Activity[]> WaitForCapturedActivities(
        ConcurrentQueue<Activity> exportedActivities,
        CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromSeconds(5);
        var startedAt = Stopwatch.GetTimestamp();

        while (true)
        {
            var capturedActivities = exportedActivities.ToArray();
            if (HasExpectedTraceShape(capturedActivities))
            {
                return capturedActivities;
            }

            if (Stopwatch.GetElapsedTime(startedAt) >= timeout)
            {
                return capturedActivities;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
        }
    }

    private static bool HasExpectedTraceShape(Activity[] capturedActivities)
    {
        var hasClientActivity = capturedActivities.Any(activity =>
            activity.Kind == ActivityKind.Client
            && string.Equals(activity.Source.Name, "System.Net.Http", StringComparison.Ordinal));

        var hasServerActivity = capturedActivities.Any(activity =>
            activity.Kind == ActivityKind.Server
            && string.Equals(activity.Source.Name, "Microsoft.AspNetCore", StringComparison.Ordinal));

        var hasDatabaseActivity = capturedActivities.Any(activity =>
            activity.Kind == ActivityKind.Client
            && activity.Tags.Any(tag =>
                string.Equals(tag.Key, "db.system.name", StringComparison.Ordinal)
                && string.Equals(tag.Value, "postgresql", StringComparison.Ordinal)));

        return hasClientActivity && hasServerActivity && hasDatabaseActivity;
    }
}
