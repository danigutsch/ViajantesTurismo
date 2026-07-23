using System.Collections.Concurrent;
using System.Diagnostics;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents.Tours;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Domain.Tours;

namespace ViajantesTurismo.Catalog.InfrastructureTests.Tours;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
public sealed class CatalogTourSlugLockPostgreSqlTests : IAsyncLifetime
{
    private CatalogTourSlugLockPostgreSqlScenario? scenario;

    private CatalogTourSlugLockPostgreSqlScenario Scenario =>
        scenario ?? throw new InvalidOperationException("Test scenario is not initialized.");

    public async ValueTask InitializeAsync()
    {
        scenario = await CatalogTourSlugLockPostgreSqlScenario.Create(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        var currentScenario = scenario;
        scenario = null;

        if (currentScenario is not null)
        {
            await currentScenario.DisposeAsync();
        }
    }

    [Fact]
    public async Task Slug_claim_completes_when_the_event_store_pool_has_one_connection()
    {
        // Arrange
        var integrationEvent = new AdminTourCreatedIntegrationEvent(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            Guid.CreateVersion7(),
            "andes-2026",
            "Andes 2026");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));

        // Act
        await Scenario.Handle(integrationEvent, timeout.Token);
        var events = await Scenario.Load(
            CatalogTourStreamIds.FromAdminTourId(integrationEvent.AdminTourId),
            TestContext.Current.CancellationToken);

        // Assert
        var envelope = events.ShouldHaveSingleItem();
        var draftCreated = envelope.Data.ShouldBeOfType<CatalogTourDraftCreated>();
        draftCreated.AdminTourId.ShouldBe(integrationEvent.AdminTourId);
    }

    [Fact]
    [Trait(TestTraitNames.CategoryName, TestTraitValues.SecurityCategory)]
    public async Task Catalog_npgsql_tracing_remains_enabled_without_first_response_or_parameter_values()
    {
        // Arrange
        var sentinel = $"private-{Guid.CreateVersion7()}";
        var stoppedActivities = new ConcurrentQueue<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = static source => string.Equals(source.Name, "Npgsql", StringComparison.Ordinal),
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            SampleUsingParentId = static (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = stoppedActivities.Enqueue,
        };
        ActivitySource.AddActivityListener(listener);
        using var parent = new Activity("catalog.npgsql.privacy-test");
        parent.SetIdFormat(ActivityIdFormat.W3C);
        parent.Start();
        var parentSpanId = parent.SpanId;
        var traceId = parent.TraceId;

        // Act
        var result = await Scenario.ExecuteParameterizedTracingProbe(
            sentinel,
            TestContext.Current.CancellationToken);

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
