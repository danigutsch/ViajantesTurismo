using PandaBear.ObjectStorage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Domain.Media;
using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.Catalog.UnitTests;

/// <summary>Arranges a reconciliation hosted service over in-memory stores for batch tests.</summary>
internal sealed class MediaObjectReconciliationHostedServiceScenario : IAsyncDisposable
{
    private readonly ServiceProvider provider;
    private readonly MediaObjectReconciliationHostedService service;

    private MediaObjectReconciliationHostedServiceScenario(
        ServiceProvider provider,
        InMemoryMediaObjectStore objectStore)
    {
        this.provider = provider;
        ObjectStore = objectStore;
        service = new MediaObjectReconciliationHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<MediaObjectReconciliationHostedService>.Instance);
    }

    /// <summary>Gets the in-memory object store the scenario runs against.</summary>
    public InMemoryMediaObjectStore ObjectStore
    {
        get;
    }

    /// <summary>Creates a scenario whose store holds an unreferenced object last modified two days ago.</summary>
    public static async Task<MediaObjectReconciliationHostedServiceScenario> CreateWithOldOrphan()
    {
        var image = PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1024);
        var objectStore = new InMemoryMediaObjectStore();
        await objectStore.Put(
            new ObjectWriteRequest(image.SourceObjectKey, new MemoryStream([1]), "image/png", 1),
            TestContext.Current.CancellationToken);
        await objectStore.Put(
            new ObjectWriteRequest("media/hosted-orphan.jpg", new MemoryStream([2]), "image/jpeg", 1),
            TestContext.Current.CancellationToken);
        objectStore.SetLastModified("media/hosted-orphan.jpg", TimeProvider.System.GetUtcNow().AddDays(-2));

        var imageStore = new InMemoryPublicMediaImageStore(image);
        var services = new ServiceCollection();
        services.AddScoped<IObjectStore>(_ => objectStore);
        services.AddScoped<IPublicMediaImageStore>(_ => imageStore);
        services.AddScoped<MediaObjectReconciliationService>();

        return new MediaObjectReconciliationHostedServiceScenario(services.BuildServiceProvider(), objectStore);
    }

    /// <summary>Creates a scenario whose store holds an unreferenced object that was just written.</summary>
    public static async Task<MediaObjectReconciliationHostedServiceScenario> CreateWithRecentOrphan()
    {
        var image = PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1024);
        var objectStore = new InMemoryMediaObjectStore();
        await objectStore.Put(
            new ObjectWriteRequest(image.SourceObjectKey, new MemoryStream([1]), "image/png", 1),
            TestContext.Current.CancellationToken);
        await objectStore.Put(
            new ObjectWriteRequest("media/recent-hosted-orphan.jpg", new MemoryStream([2]), "image/jpeg", 1),
            TestContext.Current.CancellationToken);

        return Create(image, objectStore);
    }

    /// <summary>Creates a scenario where the pending image's source object is absent from the store.</summary>
    public static MediaObjectReconciliationHostedServiceScenario CreateWithMissingObject()
    {
        var image = PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1024);
        return Create(image, new InMemoryMediaObjectStore());
    }

    private static MediaObjectReconciliationHostedServiceScenario Create(PublicMediaImage image, InMemoryMediaObjectStore objectStore)
    {
        var imageStore = new InMemoryPublicMediaImageStore(image);
        var services = new ServiceCollection();
        services.AddScoped<IObjectStore>(_ => objectStore);
        services.AddScoped<IPublicMediaImageStore>(_ => imageStore);
        services.AddScoped<MediaObjectReconciliationService>();

        return new MediaObjectReconciliationHostedServiceScenario(services.BuildServiceProvider(), objectStore);
    }

    /// <summary>Runs one reconciliation batch and returns its result.</summary>
    public ValueTask<int> ExecuteBatch(CancellationToken ct) => service.RunBatch(ct);

    /// <summary>Disposes the hosted service and its service provider.</summary>
    public async ValueTask DisposeAsync()
    {
        service.Dispose();
        await provider.DisposeAsync();
    }
}
