using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using PandaBear.EventSourcing;
using ViajantesTurismo.Catalog.Application.Projections;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Domain.Tours;
using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.Catalog.UnitTests;

/// <summary>Scenario running the projection hosted service over captured stores.</summary>
internal sealed class CatalogProjectionHostedServiceScenario : IAsyncDisposable
{
    private readonly CapturingProjectionCheckpointStore checkpointStore;
    private readonly ServiceProvider provider;
    private readonly CapturingCatalogTourReadModelStore readModelStore;
    private readonly CatalogProjectionHostedService service;

    private CatalogProjectionHostedServiceScenario(
        ServiceProvider provider,
        CapturingCatalogTourReadModelStore readModelStore,
        CapturingProjectionCheckpointStore checkpointStore)
    {
        this.provider = provider;
        this.readModelStore = readModelStore;
        this.checkpointStore = checkpointStore;
        service = new CatalogProjectionHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<CatalogProjectionHostedService>.Instance);
    }

    /// <summary>Creates a scenario whose event store replays one tour draft created event.</summary>
    public static CatalogProjectionHostedServiceScenario CreateWithOneEvent()
    {
        var eventStore = new CapturingEventStore();
        var checkpointStore = new CapturingProjectionCheckpointStore();
        var readModelStore = new CapturingCatalogTourReadModelStore();
        var projection = new CatalogTourReadModelProjection(readModelStore);
        var draftCreated = new CatalogTourDraftCreated(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "andes-2026",
            "Andes 2026",
            Guid.CreateVersion7(),
            "andes-2026");
        eventStore.AddReplayEvent(CatalogProjectionRunnerTestsHelpers.CreateEnvelope(1, draftCreated, TimeProvider.System.GetUtcNow()));

        var services = new ServiceCollection();
        services.AddScoped<IEventStore>(_ => eventStore);
        services.AddScoped<IProjectionCheckpointStore>(_ => checkpointStore);
        services.AddScoped<IProjection>(_ => projection);
        services.AddScoped<CatalogProjectionRunner>();

        return new CatalogProjectionHostedServiceScenario(services.BuildServiceProvider(), readModelStore, checkpointStore);
    }

    /// <summary>Runs one projection batch and returns its result count.</summary>
    public ValueTask<int> ExecuteBatch(CancellationToken ct) => service.RunBatch(ct);

    /// <summary>Asserts a draft was projected into the read model store.</summary>
    public void ShouldHaveProjectedDraft() => readModelStore.Draft.ShouldNotBeNull();

    /// <summary>Asserts a projection checkpoint was saved.</summary>
    public void ShouldHaveSavedCheckpoint() => checkpointStore.SavedCheckpoint.ShouldNotBeNull();

    /// <summary>Disposes the hosted service and the service provider.</summary>
    public async ValueTask DisposeAsync()
    {
        service.Dispose();
        await provider.DisposeAsync();
    }
}
