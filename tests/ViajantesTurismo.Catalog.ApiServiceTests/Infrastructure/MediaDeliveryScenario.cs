using System.Collections.Concurrent;
using PandaBear.Testing;
using ViajantesTurismo.Catalog.ApiService;
using ViajantesTurismo.Catalog.Application.Tours;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure;

/// <summary>A published tour with one reviewed 640 px JPEG rendition whose opened object streams are observable.</summary>
internal sealed class MediaDeliveryScenario : IAsyncDisposable
{
    /// <summary>Object key of the stored rendition.</summary>
    public const string ObjectKey = "stream-640.jpg";

    /// <summary>Bytes stored under <see cref="ObjectKey" />.</summary>
    public static readonly byte[] Payload = "streamed-media-payload"u8.ToArray();

    private readonly Guid imageId;
    private readonly ConcurrentQueue<PandaBear.Testing.DisposalTrackingStream> openedStreams;

    private MediaDeliveryScenario(
        WebApplicationFactory<CatalogApiHostEntryPoint> factory,
        ConcurrentQueue<PandaBear.Testing.DisposalTrackingStream> openedStreams,
        Guid imageId)
    {
        Factory = factory;
        this.openedStreams = openedStreams;
        this.imageId = imageId;
    }

    /// <summary>The host serving the scenario.</summary>
    public WebApplicationFactory<CatalogApiHostEntryPoint> Factory
    {
        get;
    }

    /// <summary>The disposal-tracking streams the object store handed out, in read order.</summary>
    public IReadOnlyList<PandaBear.Testing.DisposalTrackingStream> OpenedStreams => [.. openedStreams];

    /// <summary>Returns the public or management media route for the 640 px JPEG rendition.</summary>
    public string Route(bool isPublic) => isPublic
        ? $"/api/v1/public/catalog/media/{imageId}/640/jpg"
        : $"/api/v1/catalog/media/images/{imageId}/preview/640/jpg";

    /// <summary>
    /// Seeds the tour, image and object and returns the scenario. With <paramref name="stallReadsAfterFirstByte" />, each read
    /// serves the first payload byte and then blocks in a <see cref="BlockingReadStream" /> until it is cancelled.
    /// </summary>
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
        var openedStreams = new ConcurrentQueue<PandaBear.Testing.DisposalTrackingStream>();
        var objectStore = MediaObjectStoreTestFactory.Create((_, content) =>
        {
            var served = content;
            if (stallReadsAfterFirstByte)
            {
                content.Dispose();
                served = new BlockingReadStream(Payload[..1], Payload[1..]);
            }

            var tracked = new PandaBear.Testing.DisposalTrackingStream(served);
            openedStreams.Enqueue(tracked);
            return tracked;
        });
        objectStore.Seed(ObjectKey, Payload, "image/jpeg", "sha256:stream");

        return new MediaDeliveryScenario(CatalogApiTestHost.Create(tourStore, mediaStore, objectStore), openedStreams, image.Id);
    }

    /// <summary>Disposes the host.</summary>
    public ValueTask DisposeAsync() => Factory.DisposeAsync();
}
