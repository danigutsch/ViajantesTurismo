using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using OpenTelemetry;
using PandaBear.Testing;

namespace ViajantesTurismo.HostedTests.Catalog.Infrastructure.Tours;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
[TestScope(TestScope.Integration)]
public sealed class CatalogNpgsqlTracingTests(HostedTestFixture shared)
{
    [Fact]
    [Trait(TestTraitNames.CategoryName, TestTraitValues.SecurityCategory)]
    [SuppressMessage("Testing", "SKTEST010", Justification = "Npgsql owns the source instance; its supported instrumentation selector is its name.")]
    public async Task Catalog_npgsql_tracing_remains_enabled_without_first_response_or_parameter_values()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var scenario = await CatalogTourSlugLockPostgreSqlScenario.Create(shared, cancellationToken);
        var sentinel = $"private-{Guid.CreateVersion7()}";
        var stoppedActivities = new ConcurrentQueue<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = static source => string.Equals(source.Name, "Npgsql", StringComparison.Ordinal),
            Sample = static (ref _) => ActivitySamplingResult.AllDataAndRecorded,
            SampleUsingParentId = static (ref _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = stoppedActivities.Enqueue,
        };
        ActivitySource.AddActivityListener(listener);
        using var parent = new Activity("catalog.npgsql.privacy-test");
        parent.SetIdFormat(ActivityIdFormat.W3C);
        parent.Start();
        var parentSpanId = parent.SpanId;
        var traceId = parent.TraceId;

        // The raw listener still sees the driver span while OpenTelemetry skips export processing for this probe.
        using var suppression = SuppressInstrumentationScope.Begin();

        // Act
        var result = await scenario.ExecuteParameterizedTracingProbe(sentinel, cancellationToken);

        // Assert
        result.ShouldBe(sentinel);
        var commandActivity = stoppedActivities.ShouldHaveSingleItem(activity =>
            activity.TraceId == traceId
            && activity.ParentSpanId == parentSpanId
            && activity.GetTagItem("db.query.text") is not null);
        commandActivity.Source.Name.ShouldBe("Npgsql");
        commandActivity.Kind.ShouldBe(ActivityKind.Client);
        commandActivity.GetTagItem("db.query.text").ShouldBe("SELECT CAST(@sentinel AS text);");
        commandActivity.Events.ShouldNotContain(static activityEvent =>
            string.Equals(activityEvent.Name, "received-first-response", StringComparison.Ordinal));
        commandActivity.TagObjects.ShouldNotContain(attribute =>
            attribute.Key.Contains(sentinel, StringComparison.Ordinal)
            || (attribute.Value?.ToString()?.Contains(sentinel, StringComparison.Ordinal) ?? false));
        commandActivity.Events.SelectMany(static activityEvent => activityEvent.Tags).ShouldNotContain(attribute =>
            attribute.Key.Contains(sentinel, StringComparison.Ordinal)
            || (attribute.Value?.ToString()?.Contains(sentinel, StringComparison.Ordinal) ?? false));
    }
}
