using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SharedKernel.EventSourcing;
using ViajantesTurismo.Catalog.Application.Projections;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Domain.Tours;
using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.Catalog.UnitTests;

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
        eventStore.AddReplayEvent(CatalogProjectionRunnerTestsHelpers.CreateEnvelope(1, draftCreated, DateTimeOffset.UtcNow));

        var services = new ServiceCollection();
        services.AddScoped<IEventStore>(_ => eventStore);
        services.AddScoped<IProjectionCheckpointStore>(_ => checkpointStore);
        services.AddScoped<IProjection>(_ => projection);
        services.AddScoped<CatalogProjectionRunner>();

        return new CatalogProjectionHostedServiceScenario(services.BuildServiceProvider(), readModelStore, checkpointStore);
    }

    public ValueTask<int> ExecuteBatch(CancellationToken ct) => service.RunBatch(ct);

    public void ShouldHaveProjectedDraft() => readModelStore.Draft.ShouldNotBeNull();

    public void ShouldHaveSavedCheckpoint() => checkpointStore.SavedCheckpoint.ShouldNotBeNull();

    public async ValueTask DisposeAsync()
    {
        service.Dispose();
        await provider.DisposeAsync();
    }
}
