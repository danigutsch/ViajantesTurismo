using PandaBear.ObjectStorage;
using ViajantesTurismo.Catalog.ApiService;
using ViajantesTurismo.Catalog.Application.Tours;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure;

/// <summary>A published tour with one reviewed 640 px JPEG rendition served from an observable object store.</summary>
internal sealed class MediaDeliveryScenario : IAsyncDisposable
{
    public const string ObjectKey = "stream-640.jpg";

    public static readonly byte[] Payload = "streamed-media-payload"u8.ToArray();

    private readonly Guid imageId;

    private MediaDeliveryScenario(WebApplicationFactory<CatalogApiHostEntryPoint> factory, TestMediaObjectStore objectStore, Guid imageId)
    {
        Factory = factory;
        ObjectStore = objectStore;
        this.imageId = imageId;
    }

    public WebApplicationFactory<CatalogApiHostEntryPoint> Factory
    {
        get;
    }

    public TestMediaObjectStore ObjectStore
    {
        get;
    }

    public string Route(bool isPublic) => isPublic
        ? $"/api/v1/public/catalog/media/{imageId}/640/jpg"
        : $"/api/v1/catalog/media/images/{imageId}/preview/640/jpg";

    public static async Task<MediaDeliveryScenario> Create(bool stallReadsAfterFirstByte = false)
    {
        var tourId = Guid.CreateVersion7();
        var tourStore = new TestCatalogTourReadModelStore();
        await tourStore.UpsertDraft(
            new CatalogTourDraftReadModel(
                tourId,
                Guid.CreateVersion7(),
                "TOUR-STREAM",
                "Stream Tour",
                "stream-tour",
                true,
                1,
                TimeProvider.System.GetUtcNow()),
            TestContext.Current.CancellationToken);
        var image = PublicMediaImageTestFactory.CreateReadyImage(
            tourId,
            "stream-source.jpg",
            ObjectKey,
            "sha256:stream",
            "Stream image",
            displayOrder: 0,
            isCover: true);
        var mediaStore = new TestPublicMediaImageStore();
        await mediaStore.Upsert(image, TestContext.Current.CancellationToken);
        var objectStore = new TestMediaObjectStore { StallReadsAfterFirstByte = stallReadsAfterFirstByte };
        await objectStore.Put(
            new ObjectWriteRequest(ObjectKey, new MemoryStream(Payload), "image/jpeg", Payload.Length, "sha256:stream"),
            TestContext.Current.CancellationToken);

        return new MediaDeliveryScenario(CatalogApiTestHost.Create(tourStore, mediaStore, objectStore), objectStore, image.Id);
    }

    public ValueTask DisposeAsync() => Factory.DisposeAsync();
}
