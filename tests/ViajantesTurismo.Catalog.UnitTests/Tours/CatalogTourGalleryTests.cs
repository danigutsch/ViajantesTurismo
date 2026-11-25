using SharedKernel.Galleries;
using ViajantesTurismo.Catalog.Domain;
using ViajantesTurismo.Catalog.Domain.Tours;

namespace ViajantesTurismo.Catalog.UnitTests.Tours;

public sealed class CatalogTourGalleryTests
{
    [Fact]
    public void A_legacy_tour_retains_the_distinction_between_missing_and_explicitly_empty_placement()
    {
        // Arrange
        var created = CatalogTour.CreateDraft(Guid.CreateVersion7(), "andes-2026", "Andes 2026", Guid.CreateVersion7());
        var legacy = CatalogTour.Rehydrate(created.GetUncommittedEvents());
        var inventory = new TestGalleryPhotoInventory([]);

        // Act
        var legacyOrder = legacy.GalleryPhotoIds;
        var changed = legacy.ChangeGallery([], null, inventory);
        var eventData = legacy.GetUncommittedEvents().ShouldHaveSingleItem().ShouldBeOfType<CatalogTourGalleryChanged>();
        var replay = CatalogTour.Rehydrate([.. created.GetUncommittedEvents(), eventData]);
        var repeated = replay.ChangeGallery([], null, inventory);

        // Assert
        legacyOrder.ShouldBeNull();
        changed.IsSuccess.ShouldBeTrue();
        eventData.PhotoIds.ShouldBeEmpty();
        replay.GalleryPhotoIds.ShouldNotBeNull().ShouldBeEmpty();
        replay.GalleryCoverId.ShouldBeNull();
        repeated.IsSuccess.ShouldBeTrue();
        replay.GetUncommittedEvents().ShouldBeEmpty();
    }

    [Fact]
    public void Reordering_a_tour_gallery_snapshots_photo_IDs_and_preserves_the_chosen_cover()
    {
        // Arrange
        var created = CatalogTour.CreateDraft(Guid.CreateVersion7(), "andes-2026", "Andes 2026", Guid.CreateVersion7());
        var tour = CatalogTour.Rehydrate(created.GetUncommittedEvents());
        var first = Guid.CreateVersion7();
        var cover = Guid.CreateVersion7();
        Guid[] orderedPhotos = [first, cover];
        var inventory = new TestGalleryPhotoInventory(orderedPhotos);

        // Act
        var ordered = tour.ChangeGallery(orderedPhotos, cover, inventory);
        orderedPhotos[0] = Guid.CreateVersion7();
        var saved = tour.GetUncommittedEvents().ShouldHaveSingleItem().ShouldBeOfType<CatalogTourGalleryChanged>();
        var replay = CatalogTour.Rehydrate([.. created.GetUncommittedEvents(), saved]);
        var reordered = replay.ChangeGallery([cover, first], null, inventory);

        // Assert
        ordered.IsSuccess.ShouldBeTrue();
        saved.PhotoIds.ShouldBe([first, cover]);
        saved.CoverId.ShouldBe(cover);
        tour.GalleryPhotoIds.ShouldNotBeNull().ShouldBe([first, cover]);
        tour.GalleryCoverId.ShouldBe(cover);
        reordered.IsSuccess.ShouldBeTrue();
        replay.GalleryPhotoIds.ShouldNotBeNull().ShouldBe([cover, first]);
        replay.GalleryCoverId.ShouldBeNull();
        replay.GetUncommittedEvents().ShouldHaveSingleItem().ShouldBeOfType<CatalogTourGalleryChanged>();
    }

    [Theory]
    [InlineData("duplicate", AlbumFields.PhotoIds)]
    [InlineData("missing", AlbumFields.PhotoIds)]
    [InlineData("foreign cover", AlbumFields.CoverId)]
    [InlineData("oversized", AlbumFields.PhotoIds)]
    public void Invalid_album_placement_leaves_the_authored_state_and_events_untouched(string error, string field)
    {
        // Arrange
        var tour = CatalogTour.CreateDraft(Guid.CreateVersion7(), "andes-2026", "Andes 2026", Guid.CreateVersion7());
        var first = Guid.CreateVersion7();
        var absent = Guid.CreateVersion7();
        var inventory = new TestGalleryPhotoInventory([first]);
        tour.ChangeGallery([first], first, inventory).IsSuccess.ShouldBeTrue();
        tour.ClearUncommittedEvents();
        IReadOnlyList<Guid> invalidIds = error switch
        {
            "duplicate" => [first, first],
            "missing" => [absent],
            "oversized" => [.. Enumerable.Repeat(first, CatalogDomainLimits.MaxGalleryPhotos + 1)],
            _ => [first]
        };

        // Act
        var invalid = tour.ChangeGallery(invalidIds, error == "foreign cover" ? absent : null, inventory);

        // Assert
        invalid.IsFailure.ShouldBeTrue();
        invalid.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull().ShouldContain(entry => entry.Key == field);
        tour.GalleryPhotoIds.ShouldNotBeNull().ShouldBe([first]);
        tour.GalleryCoverId.ShouldBe(first);
        tour.GetUncommittedEvents().ShouldBeEmpty();
    }

    [Fact]
    public void Retirement_prevents_gallery_authoring()
    {
        // Arrange
        var tour = CatalogTour.CreateDraft(Guid.CreateVersion7(), "andes-2026", "Andes 2026", Guid.CreateVersion7());
        tour.Retire(tour.AdminTourId, Guid.CreateVersion7());
        tour.ClearUncommittedEvents();

        // Act
        var changed = tour.ChangeGallery([], null, new TestGalleryPhotoInventory([]));

        // Assert
        changed.IsFailure.ShouldBeTrue();
        tour.GalleryPhotoIds.ShouldBeNull();
        tour.GetUncommittedEvents().ShouldBeEmpty();
    }

    [Fact]
    public void A_published_tour_can_keep_pending_media_in_its_authored_order()
    {
        // Arrange
        var tour = CatalogTour.CreateDraft(Guid.CreateVersion7(), "andes-2026", "Andes 2026", Guid.CreateVersion7());
        tour.ChangePresentation(tour.Title, tour.Slug, "Ride the Andes.", "", "", "", "").IsSuccess.ShouldBeTrue();
        tour.Publish().IsSuccess.ShouldBeTrue();
        tour.ClearUncommittedEvents();
        var pending = Guid.CreateVersion7();

        // Act
        var changed = tour.ChangeGallery([pending], null, new TestGalleryPhotoInventory([pending]));

        // Assert
        changed.IsSuccess.ShouldBeTrue();
        tour.IsPublished.ShouldBeTrue();
        tour.GalleryPhotoIds.ShouldNotBeNull().ShouldBe([pending]);
        tour.GetUncommittedEvents().ShouldHaveSingleItem().ShouldBeOfType<CatalogTourGalleryChanged>();
    }

    [Fact]
    public void A_committed_gallery_intent_stays_acknowledged_after_event_replay()
    {
        // Arrange
        var created = CatalogTour.CreateDraft(Guid.CreateVersion7(), "andes", "Andes", Guid.CreateVersion7());
        var tour = CatalogTour.Rehydrate(created.GetUncommittedEvents());
        var intent = Guid.CreateVersion7();
        var photo = Guid.CreateVersion7();
        var inventory = new TestGalleryPhotoInventory([photo]);

        // Act
        var applied = tour.ApplyGalleryIntent([photo], photo, inventory, intent, 1);
        var gallery = tour.GetUncommittedEvents().ShouldHaveSingleItem().ShouldBeOfType<CatalogTourGalleryChanged>();
        var replay = CatalogTour.Rehydrate([.. created.GetUncommittedEvents(), gallery]);
        var repeated = replay.ApplyGalleryIntent([photo], photo, inventory, intent, 1);

        // Assert
        applied.IsSuccess.ShouldBeTrue();
        repeated.IsSuccess.ShouldBeTrue();
        replay.GalleryIntentSequence.ShouldBe(1);
        replay.GetUncommittedEvents().ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Bootstrap_leaves_unsupported_legacy_albums_untouched(bool retired)
    {
        // Arrange
        var tour = CatalogTour.CreateDraft(Guid.CreateVersion7(), "andes-2026", "Andes 2026", Guid.CreateVersion7());
        if (retired)
        {
            tour.Retire(tour.AdminTourId, Guid.CreateVersion7());
        }

        tour.ClearUncommittedEvents();
        var count = retired ? 1 : CatalogDomainLimits.MaxGalleryPhotos + 1;
        var photos = Enumerable.Range(0, count).Select(_ => Guid.CreateVersion7()).ToArray();

        // Act
        var result = tour.BootstrapLegacyGallery(photos, photos[0], new TestGalleryPhotoInventory(photos), 0);

        // Assert
        result.IsFailure.ShouldBeTrue();
        tour.GalleryPhotoIds.ShouldBeNull();
        tour.GetUncommittedEvents().ShouldBeEmpty();
    }

    [Fact]
    public void An_already_placed_upload_must_be_within_the_bootstrap_watermark()
    {
        // Arrange
        var tour = CatalogTour.CreateDraft(Guid.CreateVersion7(), "andes-2026", "Andes 2026", Guid.CreateVersion7());
        var photoId = Guid.CreateVersion7();
        var seeded = tour.BootstrapLegacyGallery([photoId], photoId, new TestGalleryPhotoInventory([photoId]), 2);
        seeded.IsSuccess.ShouldBeTrue();

        // Act
        var captured = tour.IncludesBootstrappedUpload(photoId, 2);
        var later = tour.IncludesBootstrappedUpload(photoId, 3);
        var absent = tour.IncludesBootstrappedUpload(Guid.CreateVersion7(), 1);
        tour.Retire(tour.AdminTourId, Guid.CreateVersion7());
        var retired = tour.IncludesBootstrappedUpload(photoId, 2);

        // Assert
        captured.ShouldBeTrue();
        later.ShouldBeFalse();
        absent.ShouldBeFalse();
        retired.ShouldBeFalse();
    }
}
