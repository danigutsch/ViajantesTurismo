using ViajantesTurismo.Catalog.Infrastructure.Tours;
using PandaBear.EventSourcing;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Domain.Tours;

namespace ViajantesTurismo.Catalog.UnitTests.Tours;

public sealed class CatalogTourReadModelProjectionTests
{
    [Fact]
    public void Name_identifies_the_catalog_tour_read_model_checkpoint()
    {
        // Arrange
        var projection = new CatalogTourReadModelProjection(new RecordingCatalogTourReadModelStore());

        // Assert
        projection.Name.ShouldBe("catalog.tours.read-model");
    }

    [Fact]
    public async Task Apply_rejects_a_missing_envelope()
    {
        // Arrange
        var projection = new CatalogTourReadModelProjection(new RecordingCatalogTourReadModelStore());
        Func<Task> apply = async () => await projection.Apply(null!, TestContext.Current.CancellationToken);

        // Act
        var exception = await apply.ShouldThrow<ArgumentNullException>();

        // Assert
        exception.ParamName.ShouldBe("envelope");
    }

    [Fact]
    public async Task Apply_updates_the_presentation_of_the_read_model_row()
    {
        // Arrange
        var store = new RecordingCatalogTourReadModelStore();
        var projection = new CatalogTourReadModelProjection(store);
        var catalogTourId = Guid.CreateVersion7();
        var recordedAt = TimeProvider.System.GetUtcNow();
        var presentationChanged = new CatalogTourPresentationChanged(
            catalogTourId,
            "Camino Norte",
            "camino-norte",
            "Ride the Camino Norte.",
            "A detailed customer-facing description.",
            "Day one: depart by bicycle.",
            "Camino Norte cycling tour",
            "Cycle the Camino Norte with Viajantes Turismo.");
        var envelope = new EventEnvelope(
            CatalogTourStreamIds.FromAdminTourId(Guid.CreateVersion7()),
            42,
            StreamRevision.From(3),
            Guid.CreateVersion7(),
            nameof(CatalogTourPresentationChanged),
            presentationChanged,
            recordedAt);

        // Act
        await projection.Apply(envelope, TestContext.Current.CancellationToken);

        // Assert
        var (CatalogTourId, Update, StreamVersion, Position, UpdatedAt) = store.PresentationUpdates.ShouldHaveSingleItem();
        CatalogTourId.ShouldBe(catalogTourId);
        Update.ShouldBe(new CatalogTourPresentationUpdate(
            "Camino Norte",
            "camino-norte",
            "Ride the Camino Norte.",
            "A detailed customer-facing description.",
            "Day one: depart by bicycle.",
            "Camino Norte cycling tour",
            "Cycle the Camino Norte with Viajantes Turismo."));
        StreamVersion.ShouldBe(3);
        Position.ShouldBe(42);
        UpdatedAt.ShouldBe(recordedAt);
        store.PublicationChanges.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Apply_records_publication_changes_in_the_read_model_row(bool published)
    {
        // Arrange
        var store = new RecordingCatalogTourReadModelStore();
        var projection = new CatalogTourReadModelProjection(store);
        var catalogTourId = Guid.CreateVersion7();
        var recordedAt = TimeProvider.System.GetUtcNow();
        object domainEvent = published ? new CatalogTourPublished(catalogTourId) : new CatalogTourUnpublished(catalogTourId);
        var envelope = new EventEnvelope(
            CatalogTourStreamIds.FromAdminTourId(Guid.CreateVersion7()),
            7,
            StreamRevision.From(5),
            Guid.CreateVersion7(),
            domainEvent.GetType().Name,
            domainEvent,
            recordedAt);

        // Act
        await projection.Apply(envelope, TestContext.Current.CancellationToken);

        // Assert
        var (CatalogTourId, IsPublished, StreamVersion, Position, UpdatedAt) = store.PublicationChanges.ShouldHaveSingleItem();
        CatalogTourId.ShouldBe(catalogTourId);
        IsPublished.ShouldBe(published);
        StreamVersion.ShouldBe(5);
        Position.ShouldBe(7);
        UpdatedAt.ShouldBe(recordedAt);
        store.PresentationUpdates.ShouldBeEmpty();
    }

    [Fact]
    public async Task Applying_a_gallery_event_advances_the_editor_revision()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var context = CatalogDbContextTestFactory.Create();
        var store = new EfCatalogTourReadModelStore(context);
        var projection = new CatalogTourReadModelProjection(store);
        var tour = CatalogTour.CreateDraft(Guid.CreateVersion7(), "andes", "Andes", Guid.CreateVersion7());
        var start = CatalogTourDraftReadModel.FromDraftCreated(
            tour.GetUncommittedEvents().ShouldHaveSingleItem().ShouldBeOfType<CatalogTourDraftCreated>(), 3, TimeProvider.System.GetUtcNow());
        await store.UpsertDraft(start, ct);
        var photo = Guid.CreateVersion7();
        var changed = new CatalogTourGalleryChanged(tour.Id, [photo], photo);
        var recordedAt = start.UpdatedAt.AddMinutes(1);
        var update = new EventEnvelope(CatalogTourStreamIds.FromAdminTourId(tour.AdminTourId), 7, StreamRevision.From(2),
            Guid.CreateVersion7(), "catalog.tours.gallery-changed.v1", changed, recordedAt);

        // Act
        await projection.Apply(update, ct);
        var projected = await store.GetTour(tour.Id, ct);

        // Assert
        projected.ShouldNotBeNull();
        projected.StreamVersion.ShouldBe(2);
        projected.Position.ShouldBe(7);
        projected.UpdatedAt.ShouldBe(recordedAt);
        projected.IsPublished.ShouldBeFalse();
    }

    [Fact]
    public async Task An_older_gallery_event_cannot_replace_an_explicit_empty_snapshot_or_lower_the_tour_revision()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var context = CatalogDbContextTestFactory.Create();
        var store = new EfCatalogTourReadModelStore(context);
        var projection = new CatalogTourReadModelProjection(store);
        var tour = CatalogTour.CreateDraft(Guid.CreateVersion7(), "andes", "Andes", Guid.CreateVersion7());
        var stream = CatalogTourStreamIds.FromAdminTourId(tour.AdminTourId);
        var start = CatalogTourDraftReadModel.FromDraftCreated(
            tour.GetUncommittedEvents().ShouldHaveSingleItem().ShouldBeOfType<CatalogTourDraftCreated>(), 3, TimeProvider.System.GetUtcNow());
        await store.UpsertDraft(start, ct);
        var publishedAt = start.UpdatedAt.AddMinutes(3);
        await store.SetPublicationStatus(tour.Id, true, 5, 20, publishedAt, ct);
        var oldPhoto = Guid.CreateVersion7();
        var old = new EventEnvelope(stream, 7, StreamRevision.From(2), Guid.CreateVersion7(), "catalog.tours.gallery-changed.v1",
            new CatalogTourGalleryChanged(tour.Id, [oldPhoto], oldPhoto), start.UpdatedAt.AddMinutes(1));
        var empty = new EventEnvelope(stream, 12, StreamRevision.From(3), Guid.CreateVersion7(), "catalog.tours.gallery-changed.v1",
            new CatalogTourGalleryChanged(tour.Id, [], null), start.UpdatedAt.AddMinutes(2));

        // Act
        await projection.Apply(empty, ct);
        await projection.Apply(old, ct);
        await projection.Apply(empty, ct);

        // Assert
        var current = await store.GetTour(tour.Id, ct);
        var row = current.ShouldNotBeNull();
        row.StreamVersion.ShouldBe(5);
        row.Position.ShouldBe(20);
        row.UpdatedAt.ShouldBe(publishedAt);
        row.IsPublished.ShouldBeTrue();
        row.GalleryPosition.ShouldBe(12);
        row.GalleryPhotoIds.ShouldNotBeNull().ShouldBeEmpty();
        row.GalleryCoverId.ShouldBeNull();
    }
}
