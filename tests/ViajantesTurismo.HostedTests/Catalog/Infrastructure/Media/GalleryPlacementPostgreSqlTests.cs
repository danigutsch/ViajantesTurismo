using Microsoft.EntityFrameworkCore;
using SharedKernel.Testing;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Domain.PublicContent;
using ViajantesTurismo.Catalog.Infrastructure.Tours;
using ViajantesTurismo.Catalog.Testing.Infrastructure;
using ViajantesTurismo.Catalog.Infrastructure.Media;

namespace ViajantesTurismo.HostedTests.Catalog.Infrastructure.Media;

[Trait(TestTraitNames.CategoryName, SharedKernel.IntegrationTesting.IntegrationTestTraitValues.DatabaseIntegrationCategory)]
[TestScope(TestScope.Integration)]
public sealed class GalleryPlacementPostgreSqlTests(HostedTestFixture shared) : IAsyncLifetime
{
    private GalleryPlacementPostgreSqlScenario? scenario;

    private GalleryPlacementPostgreSqlScenario Scenario =>
        scenario ?? throw new InvalidOperationException("Test scenario is not initialized.");

    public async ValueTask InitializeAsync()
    {
        scenario = await GalleryPlacementPostgreSqlScenario.Create(shared, TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        var currentScenario = scenario;
        scenario = null;

        if (currentScenario is not null)
        {
            await currentScenario.DisposeAsync();
        }
    }

    [Fact]
    public async Task Store_translates_gallery_placement_unique_constraints()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tourId = Guid.CreateVersion7();
        await Scenario.MigrateToLatest(ct);
        var first = PublicMediaImageTestFactory.CreateReadyImage(tourId, "first.jpg", "first-640.jpg", "sha256:first", "First", 0, true);
        await using var firstContext = Scenario.CreateDbContext();
        await new EfPublicMediaImageStore(firstContext).Upsert(first, ct);
        var indexNames = await Scenario.GetGalleryIndexNames(ct);

        // Assert
        indexNames.Count.ShouldBe(2);
        indexNames.ShouldContain(PublicMediaImageSchema.GalleryCoverUniqueIndex);
        indexNames.ShouldContain(PublicMediaImageSchema.GalleryDisplayOrderUniqueIndex);

        // Act and assert display-order conflict.
        var displayOrderConflict = PublicMediaImageTestFactory.CreateReadyImage(tourId, "conflict-order.jpg", "conflict-order-640.jpg", "sha256:order", "Order", 0, false);
        await using var displayOrderContext = Scenario.CreateDbContext();
        Func<Task> saveDisplayOrderConflict = () => new EfPublicMediaImageStore(displayOrderContext).Upsert(displayOrderConflict, ct).AsTask();
        await saveDisplayOrderConflict.ShouldThrow<MediaGalleryPlacementConflictException>();

        // Act and assert cover conflict.
        var coverConflict = PublicMediaImageTestFactory.CreateReadyImage(tourId, "conflict-cover.jpg", "conflict-cover-640.jpg", "sha256:cover", "Cover", 2, true);
        await using var coverContext = Scenario.CreateDbContext();
        Func<Task> saveCoverConflict = () => new EfPublicMediaImageStore(coverContext).Upsert(coverConflict, ct).AsTask();
        await saveCoverConflict.ShouldThrow<MediaGalleryPlacementConflictException>();
    }

    [Fact]
    public async Task Public_album_pages_use_reviewed_localized_placements_and_a_visible_tour()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await Scenario.MigrateToLatest(ct);
        await using var context = Scenario.CreateDbContext();
        var tourId = Guid.CreateVersion7();
        var draftId = Guid.CreateVersion7();
        var retiredId = Guid.CreateVersion7();
        var retiredAdminId = Guid.CreateVersion7();
        context.CatalogTourReadModels.AddRange(
            new()
            {
                CatalogTourId = tourId,
                AdminTourId = Guid.CreateVersion7(),
                Slug = "published-tour",
                Title = "Published",
                IsPublished = true
            },
            new()
            {
                CatalogTourId = draftId,
                AdminTourId = Guid.CreateVersion7(),
                Slug = "draft-tour",
                Title = "Draft"
            },
            new()
            {
                CatalogTourId = retiredId,
                AdminTourId = retiredAdminId,
                Slug = "retired-tour",
                Title = "Retired",
                IsPublished = true
            });
        context.CatalogTourRetirements.Add(new()
        {
            AdminTourId = retiredAdminId,
            RetirementId = Guid.CreateVersion7(),
            WithdrawnAt = TimeProvider.System.GetUtcNow()
        });
        var english = PublicContentCultures.EnglishUnitedStates;
        var portuguese = PublicContentCultures.PortugueseBrazil;
        var photos = Enumerable.Range(0, 26).Select(order => PublicMediaImageTestFactory.CreateReadyImage(
            tourId, $"source-{order}.jpg", $"variant-{order}.jpg", $"sha256:{order}", $"Photo {order}", order, order == 1)).ToArray();
        photos[0].SetReviewedAccessibilityText(portuguese, "Ciclistas", "Primeira foto", false).IsSuccess.ShouldBeTrue();
        photos[1].SetDraftAccessibilityText(portuguese, "Draft alt", "Draft caption").IsSuccess.ShouldBeTrue();
        context.PublicMediaImages.AddRange(photos);
        context.PublicMediaImages.AddRange(
            PublicMediaImageTestFactory.CreateFailedImage(tourId, 26),
            PublicMediaImageTestFactory.CreateReadyImage(draftId, "draft.jpg", "draft-640.jpg", "sha256:draft", "Draft tour", 0, true),
            PublicMediaImageTestFactory.CreateReadyImage(retiredId, "retired.jpg", "retired-640.jpg", "sha256:retired", "Retired tour", 0, true));
        await context.SaveChangesAsync(ct);
        var albums = new EfCatalogGalleryReadStore(context);

        // Act
        var englishIndex = await albums.ListAlbums(english, null, ct);
        var portugueseIndex = await albums.ListAlbums(portuguese, null, ct);
        var firstPage = await albums.GetAlbum("published-tour", portuguese, null, ct);
        var secondPage = await albums.GetAlbum("published-tour", portuguese, "2", ct);
        var outOfRange = await albums.GetAlbum("published-tour", portuguese, "3", ct);
        var retired = await albums.GetAlbum("retired-tour", english, null, ct);

        // Assert
        englishIndex.ShouldNotBeNull().Albums.ShouldHaveSingleItem().Cover.Id.ShouldBe(photos[1].Id);
        var portugueseSummary = portugueseIndex.ShouldNotBeNull().Albums.ShouldHaveSingleItem();
        portugueseSummary.Cover.Id.ShouldBe(photos[0].Id);
        portugueseSummary.Cover.AltText.ShouldBe("Ciclistas");
        portugueseSummary.Cover.Caption.ShouldBe("Primeira foto");
        portugueseSummary.PhotoCount.ShouldBe(25);
        firstPage.ShouldNotBeNull().Photos.Count.ShouldBe(24);
        firstPage.Photos.Select(photo => photo.Id).ShouldBe([photos[0].Id, .. photos.Skip(2).Take(23).Select(photo => photo.Id)]);
        firstPage.Photos[0].TextLanguage.ShouldBeNull();
        firstPage.Photos[1].AltText.ShouldBe("Photo 2");
        firstPage.Photos[1].TextLanguage.ShouldBe("en-US");
        secondPage.ShouldNotBeNull().Photos.ShouldHaveSingleItem().Id.ShouldBe(photos[25].Id);
        outOfRange.ShouldBeNull();
        retired.ShouldBeNull();
    }

    [Fact]
    public async Task Public_renditions_require_an_applied_album_placement_and_reviewed_accessibility()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await Scenario.MigrateToLatest(ct);
        await using var context = Scenario.CreateDbContext();
        var tourId = Guid.CreateVersion7();
        var visible = PublicMediaImageTestFactory.CreateReadyImage(tourId, "visible.jpg", "visible-640.jpg", "sha256:visible", "Tour photo", 0, true);
        var excluded = PublicMediaImageTestFactory.CreateReadyImage(tourId, "excluded.jpg", "excluded-640.jpg", "sha256:excluded", "Unselected photo", 1, false);
        var draft = PublicMediaImageTestFactory.CreateReadyImage(tourId, "draft.jpg", "draft-640.jpg", "sha256:draft", "Previous photo text", 2, false);
        var decorative = PublicMediaImageTestFactory.CreateReadyImage(tourId, "decorative.jpg", "decorative-640.jpg", "sha256:decorative", "Previous photo text", 3, false);
        var legacy = PublicMediaImageTestFactory.CreateReadyImage(tourId, "legacy.jpg", "legacy-640.jpg", "sha256:legacy", "Previous photo text", 4, false);
        var language = PublicContentCultures.Supported.Default;
        draft.SetDraftAccessibilityText(language, "Awaiting review", null).IsSuccess.ShouldBeTrue();
        decorative.SetReviewedAccessibilityText(language, null, null, isDecorative: true).IsSuccess.ShouldBeTrue();
        context.CatalogTourReadModels.Add(new()
        {
            CatalogTourId = tourId,
            AdminTourId = Guid.CreateVersion7(),
            Slug = "published-tour",
            Title = "Published tour",
            IsPublished = true,
            GalleryPhotoIds = [visible.Id, draft.Id, decorative.Id, legacy.Id],
            GalleryCoverId = visible.Id
        });
        context.PublicMediaImages.AddRange(visible, excluded, draft, decorative, legacy);
        await context.SaveChangesAsync(ct);
        await context.PublicMediaImages.Where(photo => photo.Id == legacy.Id)
            .ExecuteUpdateAsync(update => update.SetProperty(photo => photo.AltText, string.Empty), ct);
        var albums = new EfCatalogGalleryReadStore(context);

        // Act
        var visibleRendition = await albums.FindPublicRendition(visible.Id, 640, " IMAGE/JPEG ", ct);
        var excludedRendition = await albums.FindPublicRendition(excluded.Id, 640, "image/jpeg", ct);
        var draftRendition = await albums.FindPublicRendition(draft.Id, 640, "image/jpeg", ct);
        var decorativeRendition = await albums.FindPublicRendition(decorative.Id, 640, "image/jpeg", ct);
        var legacyRendition = await albums.FindPublicRendition(legacy.Id, 640, "image/jpeg", ct);
        var absentWidth = await albums.FindPublicRendition(visible.Id, 320, "image/jpeg", ct);
        var nonmatchingFormat = await albums.FindPublicRendition(visible.Id, 640, "image/jp%", ct);
        var missingPhoto = await albums.FindPublicRendition(Guid.CreateVersion7(), 640, "image/jpeg", ct);

        // Assert
        visibleRendition.ShouldBe(("visible-640.jpg", false, false));
        excludedRendition.ShouldBe((null, false, false));
        draftRendition.ShouldBe((null, true, false));
        decorativeRendition.ShouldBe(("decorative-640.jpg", false, false));
        legacyRendition.ShouldBe((null, true, true));
        absentWidth.ShouldBe((null, false, false));
        nonmatchingFormat.ShouldBe((null, false, false));
        missingPhoto.ShouldBe((null, false, false));
    }
}
