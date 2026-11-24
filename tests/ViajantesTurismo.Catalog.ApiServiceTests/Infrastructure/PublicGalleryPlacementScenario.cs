using PandaBear.Testing.ObjectStorage;
using ViajantesTurismo.Catalog.Api;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Domain.Media;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure;

/// <summary>Seeds public tours whose authored gallery intentionally differs from their inventory links.</summary>
internal sealed class PublicGalleryPlacementScenario(
    TestCatalogTourReadModelStore tours,
    TestPublicMediaImageStore media,
    InMemoryObjectStore objects,
    Guid omittedId,
    Guid firstId,
    Guid coverId)
{
    /// <summary>Creates a host for the public Catalog endpoints.</summary>
    public WebApplicationFactory<CatalogApiHostEntryPoint> CreateHost() => CatalogApiTestHost.Create(tours, media, objects);

    /// <summary>Gets the still-linked photo omitted from the first tour's authored gallery.</summary>
    public Guid OmittedId => omittedId;

    /// <summary>Gets the first photo in the first tour's authored order.</summary>
    public Guid FirstId => firstId;

    /// <summary>Gets the explicit cover of the first tour's authored gallery.</summary>
    public Guid CoverId => coverId;

    /// <summary>Creates two published tours with a photo shared in their media inventories.</summary>
    public static async Task<PublicGalleryPlacementScenario> Create(bool secondIncludesShared, CancellationToken ct)
    {
        var firstTourId = Guid.CreateVersion7();
        var secondTourId = Guid.CreateVersion7();
        var shared = PublicMediaImage.Create(new PublicMediaImageMetadata
        {
            Id = Guid.CreateVersion7(),
            SourceObjectKey = "shared-source.jpg",
            Checksum = "sha256:shared",
            ContentType = "image/jpeg",
            FileSizeBytes = 20,
            Dimensions = new MediaImageDimensions(640, 427),
            ProcessingStatus = MediaImageProcessingStatus.Ready,
            AltText = "Shared image"
        }, [new MediaImageResponsiveVariant("shared-640.jpg", 640, 427, "image/jpeg", 20)], [],
            [new MediaImageTourLink(firstTourId, 0, true), new MediaImageTourLink(secondTourId, 0, true)]).Value;
        var cover = PublicMediaImageTestFactory.CreateReadyImage(firstTourId, "cover.jpg", "cover-640.jpg", "sha256:cover", "Cover", 1, false);
        var first = PublicMediaImageTestFactory.CreateReadyImage(firstTourId, "first.jpg", "first-640.jpg", "sha256:first", "First", 2, false);
        var tours = new TestCatalogTourReadModelStore();
        var now = TimeProvider.System.GetUtcNow();
        await tours.UpsertDraft(new CatalogTourDraftReadModel(firstTourId, Guid.CreateVersion7(), "First", "First", "first", true, 4, now)
        {
            GalleryPhotoIds = [first.Id, cover.Id],
            GalleryCoverId = cover.Id,
            StreamVersion = 4,
            GalleryPosition = 3
        }, ct);
        await tours.UpsertDraft(new CatalogTourDraftReadModel(secondTourId, Guid.CreateVersion7(), "Second", "Second", "second", true, 4, now)
        {
            GalleryPhotoIds = secondIncludesShared ? [shared.Id] : [],
            StreamVersion = 4,
            GalleryPosition = 3
        }, ct);

        var media = new TestPublicMediaImageStore();
        await media.Upsert(shared, ct);
        await media.Upsert(cover, ct);
        await media.Upsert(first, ct);
        var objects = MediaObjectStoreTestFactory.Create();
        objects.Seed("shared-640.jpg", "approved-fixture"u8.ToArray(), "image/jpeg", "sha256:shared");
        return new PublicGalleryPlacementScenario(tours, media, objects, shared.Id, first.Id, cover.Id);
    }
}
