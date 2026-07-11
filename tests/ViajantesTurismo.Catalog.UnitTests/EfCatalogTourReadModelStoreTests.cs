using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class EfCatalogTourReadModelStoreTests
{
    [Fact]
    public async Task ListTours_returns_stable_title_ordering()
    {
        // Arrange
        await using var dbContext = EfPublicContentStoreTestDbContextFactory.Create();
        var sut = new EfCatalogTourReadModelStore(dbContext, TimeProvider.System);
        await sut.UpsertDraft(EfCatalogTourReadModelStoreTestsHelpers.CreateTour(Guid.CreateVersion7(), "Zulu"), TestContext.Current.CancellationToken);
        await sut.UpsertDraft(EfCatalogTourReadModelStoreTestsHelpers.CreateTour(Guid.CreateVersion7(), "Bravo"), TestContext.Current.CancellationToken);
        await sut.UpsertDraft(EfCatalogTourReadModelStoreTestsHelpers.CreateTour(Guid.CreateVersion7(), "Alpha"), TestContext.Current.CancellationToken);

        // Act
        var tours = await sut.ListTours(TestContext.Current.CancellationToken);

        // Assert
        (tours).ShouldMatchCollection(tour => (tour.Title).ShouldBe("Alpha"), tour => (tour.Title).ShouldBe("Bravo"), tour => (tour.Title).ShouldBe("Zulu"));
    }

    [Fact]
    public async Task GetTour_returns_matching_tour()
    {
        // Arrange
        await using var dbContext = EfPublicContentStoreTestDbContextFactory.Create();
        var sut = new EfCatalogTourReadModelStore(dbContext, TimeProvider.System);
        var tourId = Guid.CreateVersion7();
        await sut.UpsertDraft(EfCatalogTourReadModelStoreTestsHelpers.CreateTour(tourId, "Draft"), TestContext.Current.CancellationToken);

        // Act
        var tour = await sut.GetTour(tourId, TestContext.Current.CancellationToken);

        // Assert
        _ = (tour).ShouldNotBeNull();
        (tour.CatalogTourId).ShouldBe(tourId);
        (tour.Title).ShouldBe("Draft");
    }

    [Fact]
    public async Task GetTour_returns_null_when_tour_is_missing()
    {
        // Arrange
        await using var dbContext = EfPublicContentStoreTestDbContextFactory.Create();
        var sut = new EfCatalogTourReadModelStore(dbContext, TimeProvider.System);

        // Act
        var tour = await sut.GetTour(Guid.CreateVersion7(), TestContext.Current.CancellationToken);

        // Assert
        (tour).ShouldBeNull();
    }

    [Fact]
    public async Task UpdatePresentation_updates_public_fields_and_enables_slug_lookup()
    {
        // Arrange
        await using var dbContext = EfPublicContentStoreTestDbContextFactory.Create();
        var sut = new EfCatalogTourReadModelStore(dbContext, TimeProvider.System);
        var tourId = Guid.CreateVersion7();
        await sut.UpsertDraft(EfCatalogTourReadModelStoreTestsHelpers.CreateTour(tourId, "Draft"), TestContext.Current.CancellationToken);

        // Act
        var updated = await sut.UpdatePresentation(
            tourId,
            new CatalogTourPresentationUpdate("Published Tour", "published-tour", IsPublished: true),
            TestContext.Current.CancellationToken);
        var published = await sut.GetPublishedTourBySlug("published-tour", TestContext.Current.CancellationToken);

        // Assert
        _ = (updated).ShouldNotBeNull();
        (updated.Title).ShouldBe("Published Tour");
        (updated.Slug).ShouldBe("published-tour");
        (updated.IsPublished).ShouldBeTrue();
        _ = (published).ShouldNotBeNull();
        (published.CatalogTourId).ShouldBe(tourId);
    }

    [Fact]
    public async Task UpdatePresentation_trims_title_and_slug()
    {
        // Arrange
        await using var dbContext = EfPublicContentStoreTestDbContextFactory.Create();
        var sut = new EfCatalogTourReadModelStore(dbContext, TimeProvider.System);
        var tourId = Guid.CreateVersion7();
        await sut.UpsertDraft(EfCatalogTourReadModelStoreTestsHelpers.CreateTour(tourId, "Draft"), TestContext.Current.CancellationToken);

        // Act
        var updated = await sut.UpdatePresentation(
            tourId,
            new CatalogTourPresentationUpdate("  Public Title  ", "  Mixed-Case-Slug  ", IsPublished: true),
            TestContext.Current.CancellationToken);

        // Assert
        _ = (updated).ShouldNotBeNull();
        (updated.Title).ShouldBe("Public Title");
        (updated.Slug).ShouldBe("Mixed-Case-Slug");
    }

    [Fact]
    public async Task UpdatePresentation_returns_null_when_tour_is_missing()
    {
        // Arrange
        await using var dbContext = EfPublicContentStoreTestDbContextFactory.Create();
        var sut = new EfCatalogTourReadModelStore(dbContext, TimeProvider.System);

        // Act
        var updated = await sut.UpdatePresentation(
            Guid.CreateVersion7(),
            new CatalogTourPresentationUpdate("Public Title", "public-title", IsPublished: true),
            TestContext.Current.CancellationToken);

        // Assert
        (updated).ShouldBeNull();
    }

    [Fact]
    public async Task UpsertDraft_trims_slug_for_new_rows()
    {
        // Arrange
        await using var dbContext = EfPublicContentStoreTestDbContextFactory.Create();
        var sut = new EfCatalogTourReadModelStore(dbContext, TimeProvider.System);
        var tourId = Guid.CreateVersion7();

        // Act
        await sut.UpsertDraft(
            EfCatalogTourReadModelStoreTestsHelpers.CreateTour(tourId, "Draft", " mixed-slug ", isPublished: true),
            TestContext.Current.CancellationToken);
        var tour = await sut.GetTour(tourId, TestContext.Current.CancellationToken);

        // Assert
        _ = (tour).ShouldNotBeNull();
        (tour.Slug).ShouldBe("mixed-slug");
    }

    [Fact]
    public async Task GetPublishedTourBySlug_trims_lookup_slug()
    {
        // Arrange
        await using var dbContext = EfPublicContentStoreTestDbContextFactory.Create();
        var sut = new EfCatalogTourReadModelStore(dbContext, TimeProvider.System);
        var tourId = Guid.CreateVersion7();
        await sut.UpsertDraft(
            EfCatalogTourReadModelStoreTestsHelpers.CreateTour(tourId, "Draft", "mixed-slug", isPublished: true),
            TestContext.Current.CancellationToken);

        // Act
        var tour = await sut.GetPublishedTourBySlug(" mixed-slug ", TestContext.Current.CancellationToken);

        // Assert
        _ = (tour).ShouldNotBeNull();
        (tour.CatalogTourId).ShouldBe(tourId);
    }

    [Fact]
    public async Task GetPublishedTourBySlug_uses_exact_slug_casing()
    {
        // Arrange
        await using var dbContext = EfPublicContentStoreTestDbContextFactory.Create();
        var sut = new EfCatalogTourReadModelStore(dbContext, TimeProvider.System);
        await sut.UpsertDraft(
            EfCatalogTourReadModelStoreTestsHelpers.CreateTour(Guid.CreateVersion7(), "Draft", "Mixed-Slug", isPublished: true),
            TestContext.Current.CancellationToken);

        // Act
        var tour = await sut.GetPublishedTourBySlug("mixed-slug", TestContext.Current.CancellationToken);

        // Assert
        (tour).ShouldBeNull();
    }

    [Fact]
    public async Task GetPublishedTourBySlug_returns_null_when_tour_is_unpublished()
    {
        // Arrange
        await using var dbContext = EfPublicContentStoreTestDbContextFactory.Create();
        var sut = new EfCatalogTourReadModelStore(dbContext, TimeProvider.System);
        await sut.UpsertDraft(
            EfCatalogTourReadModelStoreTestsHelpers.CreateTour(Guid.CreateVersion7(), "Draft", "DRAFT", isPublished: false),
            TestContext.Current.CancellationToken);

        // Act
        var tour = await sut.GetPublishedTourBySlug("draft", TestContext.Current.CancellationToken);

        // Assert
        (tour).ShouldBeNull();
    }

    [Fact]
    public async Task UpsertDraft_preserves_existing_presentation_fields()
    {
        // Arrange
        await using var dbContext = EfPublicContentStoreTestDbContextFactory.Create();
        var sut = new EfCatalogTourReadModelStore(dbContext, TimeProvider.System);
        var tourId = Guid.CreateVersion7();
        await sut.UpsertDraft(EfCatalogTourReadModelStoreTestsHelpers.CreateTour(tourId, "Draft"), TestContext.Current.CancellationToken);
        await sut.UpdatePresentation(
            tourId,
            new CatalogTourPresentationUpdate("Public Title", "public-title", IsPublished: true),
            TestContext.Current.CancellationToken);

        // Act
        await sut.UpsertDraft(EfCatalogTourReadModelStoreTestsHelpers.CreateTour(tourId, "Projection Title"), TestContext.Current.CancellationToken);
        var tours = await sut.ListTours(TestContext.Current.CancellationToken);

        // Assert
        var tour = (tours).ShouldHaveSingleItem();
        (tour.Title).ShouldBe("Public Title");
        (tour.Slug).ShouldBe("public-title");
        (tour.IsPublished).ShouldBeTrue();
    }

}
