using ViajantesTurismo.Catalog.Api;
using ViajantesTurismo.Catalog.Application.Gallery;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Domain.Tours;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure;

/// <summary>Provides the real editor routes with an isolated Catalog journal and seeded tour.</summary>
internal sealed class CatalogGalleryWriteScenario(
    TestCatalogTourReadModelStore tours,
    TestPublicMediaImageStore images,
    Guid tourId,
    Guid firstPhotoId,
    Guid secondPhotoId,
    CatalogGalleryWriteIntentStore journal,
    TestEventStore events)
{
    /// <summary>Gets the seeded tour.</summary>
    public Guid TourId => tourId;

    /// <summary>Gets the original first photo.</summary>
    public Guid FirstPhotoId => firstPhotoId;

    /// <summary>Gets the original second photo.</summary>
    public Guid SecondPhotoId => secondPhotoId;

    /// <summary>Gets the accepted placement records.</summary>
    public ICatalogGalleryPlacementIntentStore Journal => journal;

    /// <summary>Gets the Catalog gallery editor request path.</summary>
    public Uri EditorPath => new($"/api/v1/catalog/tours/{tourId}/gallery-editor", UriKind.Relative);

    /// <summary>Creates the API host with the test's seeded tour and journal.</summary>
    public WebApplicationFactory<CatalogApiHostEntryPoint> CreateHost() =>
        CatalogApiTestHost.CreateWithGalleryJournal(tours, images, journal, events);

    /// <summary>Rejects the next in-request gallery projection after its event is committed.</summary>
    public void FailNextGalleryProjection() => tours.FailNextGalleryProjection = true;

    /// <summary>Replays the single committed gallery change through the ordinary tour projection.</summary>
    public async Task ProjectCommittedGallery(CancellationToken ct)
    {
        var tour = await tours.GetTour(TourId, ct) ?? throw new InvalidOperationException("The seeded tour is missing.");
        var stream = await events.Load(CatalogTourStreamIds.FromAdminTourId(tour.AdminTourId), afterRevision: null, ct);
        var gallery = stream.Single(envelope => envelope.Data is CatalogTourGalleryChanged);
        await new CatalogTourReadModelProjection(tours, journal).Apply(gallery, ct);
    }

    /// <summary>Creates a draft or published tour with two existing photos on the actual Catalog API host.</summary>
    public static async Task<CatalogGalleryWriteScenario> Create(bool published, CancellationToken ct)
    {
        var tourId = Guid.CreateVersion7();
        var revision = published ? 3 : 1;
        var tours = new TestCatalogTourReadModelStore();
        var tour = new CatalogTourDraftReadModel(
            tourId, Guid.CreateVersion7(), "andes", "Andes", "andes", published, revision, TimeProvider.System.GetUtcNow())
        {
            StreamVersion = revision,
            Summary = "Ride the Andes."
        };
        await tours.UpsertDraft(tour, ct);
        var events = new TestEventStore();
        events.SeedTour(tour);
        var images = new TestPublicMediaImageStore();
        var first = PublicMediaImageTestFactory.CreateReadyImage(tourId, "first.jpg", "first-640.jpg", "sha256:first", "First", 0, true);
        var second = PublicMediaImageTestFactory.CreateReadyImage(tourId, "second.jpg", "second-640.jpg", "sha256:second", "Second", 1, false);
        await images.Upsert(first, ct);
        await images.Upsert(second, ct);
        return new CatalogGalleryWriteScenario(tours, images, tourId, first.Id, second.Id, new CatalogGalleryWriteIntentStore(), events);
    }
}
