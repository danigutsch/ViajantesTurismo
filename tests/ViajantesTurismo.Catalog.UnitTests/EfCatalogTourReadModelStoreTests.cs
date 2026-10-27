using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class EfCatalogTourReadModelStoreTests
{
    [Fact]
    [Trait(PandaBear.Testing.TestTraitNames.CategoryName, PandaBear.Testing.TestTraitValues.SecurityCategory)]
    public async Task Tour_reload_observes_public_visibility_committed_while_waiting_for_a_lease()
    {
        // Arrange
        var database = Guid.CreateVersion7().ToString("N");
        var root = new InMemoryDatabaseRoot();
        await using var firstContext = CatalogDbContextTestFactory.Create(database, root);
        await using var secondContext = CatalogDbContextTestFactory.Create(database, root);
        var firstStore = new EfCatalogTourReadModelStore(firstContext);
        var secondStore = new EfCatalogTourReadModelStore(secondContext);
        var tour = EfCatalogTourReadModelStoreTestsHelpers.CreateTour(Guid.CreateVersion7(), "Tour") with
        {
            Summary = "Ready for public delivery"
        };
        await firstStore.UpsertDraft(tour, TestContext.Current.CancellationToken);
        var beforeWait = await firstStore.GetTour(tour.CatalogTourId, TestContext.Current.CancellationToken);
        beforeWait.ShouldNotBeNull();
        _ = await secondStore.SetPublicationStatus(tour.CatalogTourId, true, 2, 2, TimeProvider.System.GetUtcNow(), TestContext.Current.CancellationToken);

        // Act
        var afterWait = await firstStore.GetTour(tour.CatalogTourId, TestContext.Current.CancellationToken);

        // Assert
        afterWait.ShouldNotBeNull();
        afterWait.IsPubliclyVisible.ShouldBeTrue();
        afterWait.StreamVersion.ShouldBe(2);
    }

    [Fact]
    public async Task ListTours_returns_stable_title_ordering()
    {
        // Arrange
        await using var dbContext = EfPublicContentStoreTestDbContextFactory.Create();
        var sut = new EfCatalogTourReadModelStore(dbContext);
        await sut.UpsertDraft(EfCatalogTourReadModelStoreTestsHelpers.CreateTour(Guid.CreateVersion7(), "Zulu"), TestContext.Current.CancellationToken);
        await sut.UpsertDraft(EfCatalogTourReadModelStoreTestsHelpers.CreateTour(Guid.CreateVersion7(), "Bravo"), TestContext.Current.CancellationToken);
        await sut.UpsertDraft(EfCatalogTourReadModelStoreTestsHelpers.CreateTour(Guid.CreateVersion7(), "Alpha"), TestContext.Current.CancellationToken);

        // Act
        var tours = await sut.ListTours(TestContext.Current.CancellationToken);

        // Assert
        tours.ShouldMatchCollection(tour => tour.Title.ShouldBe("Alpha"), tour => tour.Title.ShouldBe("Bravo"), tour => tour.Title.ShouldBe("Zulu"));
    }

    [Fact]
    public async Task GetTour_returns_matching_tour()
    {
        // Arrange
        await using var dbContext = EfPublicContentStoreTestDbContextFactory.Create();
        var sut = new EfCatalogTourReadModelStore(dbContext);
        var tourId = Guid.CreateVersion7();
        await sut.UpsertDraft(EfCatalogTourReadModelStoreTestsHelpers.CreateTour(tourId, "Draft"), TestContext.Current.CancellationToken);

        // Act
        var tour = await sut.GetTour(tourId, TestContext.Current.CancellationToken);

        // Assert
        _ = tour.ShouldNotBeNull();
        tour.CatalogTourId.ShouldBe(tourId);
        tour.Title.ShouldBe("Draft");
    }

    [Fact]
    public async Task GetTour_returns_null_when_tour_is_missing()
    {
        // Arrange
        await using var dbContext = EfPublicContentStoreTestDbContextFactory.Create();
        var sut = new EfCatalogTourReadModelStore(dbContext);

        // Act
        var tour = await sut.GetTour(Guid.CreateVersion7(), TestContext.Current.CancellationToken);

        // Assert
        tour.ShouldBeNull();
    }

    [Fact]
    public async Task Presentation_and_publication_status_round_trip_public_fields()
    {
        // Arrange
        await using var dbContext = EfPublicContentStoreTestDbContextFactory.Create();
        var sut = new EfCatalogTourReadModelStore(dbContext);
        var tourId = Guid.CreateVersion7();
        var updatedAt = TimeProvider.System.GetUtcNow();
        await sut.UpsertDraft(EfCatalogTourReadModelStoreTestsHelpers.CreateTour(tourId, "Draft"), TestContext.Current.CancellationToken);

        // Act
        var updated = await sut.UpdatePresentation(
            tourId,
            new CatalogTourPresentationUpdate(
                "Published Tour",
                "published-tour",
                "Tour summary",
                "Tour description",
                "Day one: ride.",
                "Published Tour SEO",
                "Published tour search description."),
            streamVersion: 2,
            position: 2,
            updatedAt,
            ct: TestContext.Current.CancellationToken);
        _ = await sut.SetPublicationStatus(
            tourId,
            isPublished: true,
            streamVersion: 3,
            position: 3,
            updatedAt: updatedAt.AddMinutes(1),
            ct: TestContext.Current.CancellationToken);
        var published = await sut.GetPublishedTourBySlug("published-tour", TestContext.Current.CancellationToken);

        // Assert
        _ = updated.ShouldNotBeNull();
        updated.Title.ShouldBe("Published Tour");
        updated.Slug.ShouldBe("published-tour");
        updated.Summary.ShouldBe("Tour summary");
        updated.Description.ShouldBe("Tour description");
        updated.Itinerary.ShouldBe("Day one: ride.");
        updated.SeoTitle.ShouldBe("Published Tour SEO");
        updated.SeoDescription.ShouldBe("Published tour search description.");
        updated.IsPublished.ShouldBeFalse();
        _ = published.ShouldNotBeNull();
        published.CatalogTourId.ShouldBe(tourId);
        published.StreamVersion.ShouldBe(3);
    }

    [Fact]
    public async Task Published_row_is_public_without_summary_or_stream_history()
    {
        // Arrange
        await using var dbContext = EfPublicContentStoreTestDbContextFactory.Create();
        var sut = new EfCatalogTourReadModelStore(dbContext);
        var tourId = Guid.CreateVersion7();
        var tour = EfCatalogTourReadModelStoreTestsHelpers.CreateTour(
            tourId,
            "Published Tour",
            "published-tour",
            isPublished: true) with
        {
            StreamVersion = 1,
            Summary = string.Empty
        };
        await sut.UpsertDraft(tour, TestContext.Current.CancellationToken);

        // Act
        var stored = await sut.GetTour(tourId, TestContext.Current.CancellationToken);
        var published = await sut.GetPublishedTourBySlug(tour.Slug, TestContext.Current.CancellationToken);

        // Assert
        stored.ShouldNotBeNull().IsPublished.ShouldBeTrue();
        published.ShouldNotBeNull().CatalogTourId.ShouldBe(tourId);
    }

    [Fact]
    public async Task Out_of_order_partial_projections_preserve_presentation_and_publication_fields()
    {
        // Arrange
        await using var dbContext = EfPublicContentStoreTestDbContextFactory.Create();
        var sut = new EfCatalogTourReadModelStore(dbContext);
        var tourId = Guid.CreateVersion7();
        await sut.UpsertDraft(
            EfCatalogTourReadModelStoreTestsHelpers.CreateTour(tourId, "Draft", "draft-tour", isPublished: false),
            TestContext.Current.CancellationToken);
        _ = await sut.SetPublicationStatus(
            tourId,
            isPublished: true,
            streamVersion: 3,
            position: 3,
            updatedAt: TimeProvider.System.GetUtcNow(),
            ct: TestContext.Current.CancellationToken);

        // Act
        _ = await sut.UpdatePresentation(
            tourId,
            new CatalogTourPresentationUpdate("Published Tour", "published-tour", "Tour summary", "", "", "", ""),
            streamVersion: 2,
            position: 2,
            updatedAt: TimeProvider.System.GetUtcNow(),
            ct: TestContext.Current.CancellationToken);
        var projected = await sut.GetTour(tourId, TestContext.Current.CancellationToken);

        // Assert
        projected.ShouldNotBeNull();
        projected.Title.ShouldBe("Published Tour");
        projected.Summary.ShouldBe("Tour summary");
        projected.IsPublished.ShouldBeTrue();
        projected.StreamVersion.ShouldBe(3);
        projected.Position.ShouldBe(3);
    }

    [Fact]
    public async Task Presentation_after_an_unprojected_unpublish_clears_stale_publication()
    {
        // Arrange
        await using var dbContext = EfPublicContentStoreTestDbContextFactory.Create();
        var sut = new EfCatalogTourReadModelStore(dbContext);
        var tourId = Guid.CreateVersion7();
        await sut.UpsertDraft(
            EfCatalogTourReadModelStoreTestsHelpers.CreateTour(tourId, "Draft", "draft-tour", isPublished: false),
            TestContext.Current.CancellationToken);
        _ = await sut.UpdatePresentation(
            tourId,
            new CatalogTourPresentationUpdate("Published Tour", "published-tour", "Tour summary", "", "", "", ""),
            streamVersion: 2,
            position: 2,
            updatedAt: TimeProvider.System.GetUtcNow(),
            ct: TestContext.Current.CancellationToken);
        _ = await sut.SetPublicationStatus(
            tourId,
            isPublished: true,
            streamVersion: 3,
            position: 3,
            updatedAt: TimeProvider.System.GetUtcNow(),
            ct: TestContext.Current.CancellationToken);

        // Act
        var projected = await sut.UpdatePresentation(
            tourId,
            new CatalogTourPresentationUpdate("Edited Draft", "edited-draft", "Edited summary", "", "", "", ""),
            streamVersion: 5,
            position: 5,
            updatedAt: TimeProvider.System.GetUtcNow(),
            ct: TestContext.Current.CancellationToken);
        var publicTour = await sut.GetPublishedTourBySlug("edited-draft", TestContext.Current.CancellationToken);

        // Assert
        projected.ShouldNotBeNull();
        projected.IsPublished.ShouldBeFalse();
        projected.StreamVersion.ShouldBe(5);
        projected.Position.ShouldBe(5);
        publicTour.ShouldBeNull();
    }

    [Fact]
    public async Task Delayed_publish_before_a_newer_presentation_does_not_restore_stale_publication()
    {
        // Arrange
        await using var dbContext = EfPublicContentStoreTestDbContextFactory.Create();
        var sut = new EfCatalogTourReadModelStore(dbContext);
        var tourId = Guid.CreateVersion7();
        await sut.UpsertDraft(
            EfCatalogTourReadModelStoreTestsHelpers.CreateTour(tourId, "Draft", "draft-tour", isPublished: false),
            TestContext.Current.CancellationToken);
        _ = await sut.UpdatePresentation(
            tourId,
            new CatalogTourPresentationUpdate("Edited Draft", "edited-draft", "Edited summary", "", "", "", ""),
            streamVersion: 5,
            position: 5,
            updatedAt: TimeProvider.System.GetUtcNow(),
            ct: TestContext.Current.CancellationToken);

        // Act
        var projected = await sut.SetPublicationStatus(
            tourId,
            isPublished: true,
            streamVersion: 3,
            position: 3,
            updatedAt: TimeProvider.System.GetUtcNow(),
            ct: TestContext.Current.CancellationToken);
        var publicTour = await sut.GetPublishedTourBySlug("edited-draft", TestContext.Current.CancellationToken);

        // Assert
        projected.ShouldNotBeNull();
        projected.IsPublished.ShouldBeFalse();
        projected.StreamVersion.ShouldBe(5);
        projected.Position.ShouldBe(5);
        publicTour.ShouldBeNull();
    }

    [Fact]
    public async Task UpdatePresentation_trims_title_and_slug()
    {
        // Arrange
        await using var dbContext = EfPublicContentStoreTestDbContextFactory.Create();
        var sut = new EfCatalogTourReadModelStore(dbContext);
        var tourId = Guid.CreateVersion7();
        await sut.UpsertDraft(EfCatalogTourReadModelStoreTestsHelpers.CreateTour(tourId, "Draft"), TestContext.Current.CancellationToken);

        // Act
        var updated = await sut.UpdatePresentation(
            tourId,
            new CatalogTourPresentationUpdate("  Public Title  ", "  Mixed-Case-Slug  ", "", "", "", "", ""),
            streamVersion: 2,
            position: 2,
            updatedAt: TimeProvider.System.GetUtcNow(),
            ct: TestContext.Current.CancellationToken);

        // Assert
        _ = updated.ShouldNotBeNull();
        updated.Title.ShouldBe("Public Title");
        updated.Slug.ShouldBe("Mixed-Case-Slug");
    }

    [Fact]
    public async Task Presentation_watermark_prevents_an_older_projection_from_overwriting_a_newer_one()
    {
        // Arrange
        var databaseName = Guid.NewGuid().ToString();
        var databaseRoot = new InMemoryDatabaseRoot();
        var tourId = Guid.CreateVersion7();
        await using (var seedContext = EfPublicContentStoreTestDbContextFactory.Create(databaseName, databaseRoot))
        {
            var seedStore = new EfCatalogTourReadModelStore(seedContext);
            await seedStore.UpsertDraft(
                EfCatalogTourReadModelStoreTestsHelpers.CreateTour(tourId, "Draft"),
                TestContext.Current.CancellationToken);
            var seedProjection = await seedContext.CatalogTourReadModels.SingleAsync(TestContext.Current.CancellationToken);
            seedProjection.Position = 5;
            seedProjection.PublicationPosition = 5;
            await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var newerContext = EfPublicContentStoreTestDbContextFactory.Create(databaseName, databaseRoot);
        await using var olderContext = EfPublicContentStoreTestDbContextFactory.Create(databaseName, databaseRoot);
        var newerProjection = await newerContext.CatalogTourReadModels.SingleAsync(TestContext.Current.CancellationToken);
        var olderProjection = await olderContext.CatalogTourReadModels.SingleAsync(TestContext.Current.CancellationToken);
        newerProjection.Title = "Newer projection";
        newerProjection.PresentationPosition = 3;
        olderProjection.Title = "Older projection";
        olderProjection.PresentationPosition = 2;
        await newerContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        Func<Task> saveOlderProjection = () => olderContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await saveOlderProjection.ShouldThrow<DbUpdateConcurrencyException>();
        await using var verificationContext = EfPublicContentStoreTestDbContextFactory.Create(databaseName, databaseRoot);
        var persisted = await verificationContext.CatalogTourReadModels.SingleAsync(TestContext.Current.CancellationToken);
        persisted.Title.ShouldBe("Newer projection");
        persisted.PresentationPosition.ShouldBe(3);
        persisted.Position.ShouldBe(5);
    }

    [Fact]
    public async Task Publication_watermark_prevents_an_older_projection_from_overwriting_a_newer_one()
    {
        // Arrange
        var databaseName = Guid.NewGuid().ToString();
        var databaseRoot = new InMemoryDatabaseRoot();
        var tourId = Guid.CreateVersion7();
        await using (var seedContext = EfPublicContentStoreTestDbContextFactory.Create(databaseName, databaseRoot))
        {
            var seedStore = new EfCatalogTourReadModelStore(seedContext);
            await seedStore.UpsertDraft(
                EfCatalogTourReadModelStoreTestsHelpers.CreateTour(tourId, "Draft"),
                TestContext.Current.CancellationToken);
            var seedProjection = await seedContext.CatalogTourReadModels.SingleAsync(TestContext.Current.CancellationToken);
            seedProjection.Position = 5;
            seedProjection.PresentationPosition = 5;
            await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var newerContext = EfPublicContentStoreTestDbContextFactory.Create(databaseName, databaseRoot);
        await using var olderContext = EfPublicContentStoreTestDbContextFactory.Create(databaseName, databaseRoot);
        var newerProjection = await newerContext.CatalogTourReadModels.SingleAsync(TestContext.Current.CancellationToken);
        var olderProjection = await olderContext.CatalogTourReadModels.SingleAsync(TestContext.Current.CancellationToken);
        newerProjection.IsPublished = true;
        newerProjection.PublicationPosition = 3;
        olderProjection.IsPublished = false;
        olderProjection.PublicationPosition = 2;
        await newerContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        Func<Task> saveOlderProjection = () => olderContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await saveOlderProjection.ShouldThrow<DbUpdateConcurrencyException>();
        await using var verificationContext = EfPublicContentStoreTestDbContextFactory.Create(databaseName, databaseRoot);
        var persisted = await verificationContext.CatalogTourReadModels.SingleAsync(TestContext.Current.CancellationToken);
        persisted.IsPublished.ShouldBeTrue();
        persisted.PublicationPosition.ShouldBe(3);
        persisted.Position.ShouldBe(5);
    }

    [Fact]
    public async Task UpdatePresentation_returns_null_when_tour_is_missing()
    {
        // Arrange
        await using var dbContext = EfPublicContentStoreTestDbContextFactory.Create();
        var sut = new EfCatalogTourReadModelStore(dbContext);

        // Act
        var updated = await sut.UpdatePresentation(
            Guid.CreateVersion7(),
            new CatalogTourPresentationUpdate("Public Title", "public-title", "", "", "", "", ""),
            streamVersion: 2,
            position: 2,
            updatedAt: TimeProvider.System.GetUtcNow(),
            ct: TestContext.Current.CancellationToken);

        // Assert
        updated.ShouldBeNull();
    }

    [Fact]
    public async Task UpsertDraft_trims_slug_for_new_rows()
    {
        // Arrange
        await using var dbContext = EfPublicContentStoreTestDbContextFactory.Create();
        var sut = new EfCatalogTourReadModelStore(dbContext);
        var tourId = Guid.CreateVersion7();

        // Act
        await sut.UpsertDraft(
            EfCatalogTourReadModelStoreTestsHelpers.CreateTour(tourId, "Draft", " mixed-slug ", isPublished: true),
            TestContext.Current.CancellationToken);
        var tour = await sut.GetTour(tourId, TestContext.Current.CancellationToken);

        // Assert
        _ = tour.ShouldNotBeNull();
        tour.Slug.ShouldBe("mixed-slug");
    }

    [Fact]
    public async Task GetPublishedTourBySlug_trims_lookup_slug()
    {
        // Arrange
        await using var dbContext = EfPublicContentStoreTestDbContextFactory.Create();
        var sut = new EfCatalogTourReadModelStore(dbContext);
        var tourId = Guid.CreateVersion7();
        await sut.UpsertDraft(
            EfCatalogTourReadModelStoreTestsHelpers.CreateTour(tourId, "Draft", "mixed-slug", isPublished: true) with
            {
                Summary = "Tour summary",
                StreamVersion = 3
            },
            TestContext.Current.CancellationToken);

        // Act
        var tour = await sut.GetPublishedTourBySlug(" mixed-slug ", TestContext.Current.CancellationToken);

        // Assert
        _ = tour.ShouldNotBeNull();
        tour.CatalogTourId.ShouldBe(tourId);
    }

    [Fact]
    public async Task GetPublishedTourBySlug_uses_exact_slug_casing()
    {
        // Arrange
        await using var dbContext = EfPublicContentStoreTestDbContextFactory.Create();
        var sut = new EfCatalogTourReadModelStore(dbContext);
        await sut.UpsertDraft(
            EfCatalogTourReadModelStoreTestsHelpers.CreateTour(Guid.CreateVersion7(), "Draft", "Mixed-Slug", isPublished: true),
            TestContext.Current.CancellationToken);

        // Act
        var tour = await sut.GetPublishedTourBySlug("mixed-slug", TestContext.Current.CancellationToken);

        // Assert
        tour.ShouldBeNull();
    }

    [Fact]
    public async Task GetPublishedTourBySlug_returns_null_when_tour_is_unpublished()
    {
        // Arrange
        await using var dbContext = EfPublicContentStoreTestDbContextFactory.Create();
        var sut = new EfCatalogTourReadModelStore(dbContext);
        await sut.UpsertDraft(
            EfCatalogTourReadModelStoreTestsHelpers.CreateTour(Guid.CreateVersion7(), "Draft", "DRAFT", isPublished: false),
            TestContext.Current.CancellationToken);

        // Act
        var tour = await sut.GetPublishedTourBySlug("draft", TestContext.Current.CancellationToken);

        // Assert
        tour.ShouldBeNull();
    }

    [Fact]
    public async Task UpsertDraft_preserves_existing_presentation_fields()
    {
        // Arrange
        await using var dbContext = EfPublicContentStoreTestDbContextFactory.Create();
        var sut = new EfCatalogTourReadModelStore(dbContext);
        var tourId = Guid.CreateVersion7();
        await sut.UpsertDraft(EfCatalogTourReadModelStoreTestsHelpers.CreateTour(tourId, "Draft"), TestContext.Current.CancellationToken);
        await sut.UpdatePresentation(
            tourId,
            new CatalogTourPresentationUpdate(
                "Public Title",
                "public-title",
                "Tour summary",
                "Tour description",
                "Tour itinerary",
                "Tour SEO title",
                "Tour SEO description"),
            streamVersion: 2,
            position: 2,
            updatedAt: TimeProvider.System.GetUtcNow(),
            ct: TestContext.Current.CancellationToken);
        _ = await sut.SetPublicationStatus(
            tourId,
            isPublished: true,
            streamVersion: 3,
            position: 3,
            updatedAt: TimeProvider.System.GetUtcNow(),
            ct: TestContext.Current.CancellationToken);

        // Act
        await sut.UpsertDraft(EfCatalogTourReadModelStoreTestsHelpers.CreateTour(tourId, "Projection Title"), TestContext.Current.CancellationToken);
        var tours = await sut.ListTours(TestContext.Current.CancellationToken);

        // Assert
        var tour = tours.ShouldHaveSingleItem();
        tour.Title.ShouldBe("Public Title");
        tour.Slug.ShouldBe("public-title");
        tour.Summary.ShouldBe("Tour summary");
        tour.Description.ShouldBe("Tour description");
        tour.Itinerary.ShouldBe("Tour itinerary");
        tour.SeoTitle.ShouldBe("Tour SEO title");
        tour.SeoDescription.ShouldBe("Tour SEO description");
        tour.IsPublished.ShouldBeTrue();
    }

}
