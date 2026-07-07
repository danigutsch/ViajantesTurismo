using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Domain.Media;
using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.Catalog.UnitTests;

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

    public InMemoryMediaObjectStore ObjectStore { get; }

    public static async Task<MediaObjectReconciliationHostedServiceScenario> CreateWithOldOrphan()
    {
        var image = PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1024);
        var objectStore = new InMemoryMediaObjectStore();
        await objectStore.Put(
            new MediaObjectWriteRequest(image.SourceObjectKey, new MemoryStream([1]), "image/png", 1),
            TestContext.Current.CancellationToken);
        await objectStore.Put(
            new MediaObjectWriteRequest("media/hosted-orphan.jpg", new MemoryStream([2]), "image/jpeg", 1),
            TestContext.Current.CancellationToken);
        objectStore.SetLastModified("media/hosted-orphan.jpg", DateTimeOffset.UtcNow.AddDays(-2));

        var imageStore = new InMemoryPublicMediaImageStore(image);
        var services = new ServiceCollection();
        services.AddScoped<IMediaObjectStore>(_ => objectStore);
        services.AddScoped<IPublicMediaImageStore>(_ => imageStore);
        services.AddScoped<MediaObjectReconciliationService>();

        return new MediaObjectReconciliationHostedServiceScenario(services.BuildServiceProvider(), objectStore);
    }

    public static async Task<MediaObjectReconciliationHostedServiceScenario> CreateWithRecentOrphan()
    {
        var image = PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1024);
        var objectStore = new InMemoryMediaObjectStore();
        await objectStore.Put(
            new MediaObjectWriteRequest(image.SourceObjectKey, new MemoryStream([1]), "image/png", 1),
            TestContext.Current.CancellationToken);
        await objectStore.Put(
            new MediaObjectWriteRequest("media/recent-hosted-orphan.jpg", new MemoryStream([2]), "image/jpeg", 1),
            TestContext.Current.CancellationToken);

        return Create(image, objectStore);
    }

    public static MediaObjectReconciliationHostedServiceScenario CreateWithMissingObject()
    {
        var image = PublicMediaImageTestFactory.CreatePendingImage(Guid.CreateVersion7(), 1024);
        return Create(image, new InMemoryMediaObjectStore());
    }

    private static MediaObjectReconciliationHostedServiceScenario Create(PublicMediaImage image, InMemoryMediaObjectStore objectStore)
    {
        var imageStore = new InMemoryPublicMediaImageStore(image);
        var services = new ServiceCollection();
        services.AddScoped<IMediaObjectStore>(_ => objectStore);
        services.AddScoped<IPublicMediaImageStore>(_ => imageStore);
        services.AddScoped<MediaObjectReconciliationService>();

        return new MediaObjectReconciliationHostedServiceScenario(services.BuildServiceProvider(), objectStore);
    }

    public ValueTask<int> ExecuteBatch(CancellationToken ct) => service.RunBatch(ct);

    public async ValueTask DisposeAsync()
    {
        service.Dispose();
        await provider.DisposeAsync();
    }
}
