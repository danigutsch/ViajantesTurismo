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
        var sut = new EfCatalogTourReadModelStore(dbContext);
        await sut.UpsertDraft(EfCatalogTourReadModelStoreTestsHelpers.CreateTour(Guid.CreateVersion7(), "Zulu"), TestContext.Current.CancellationToken);
        await sut.UpsertDraft(EfCatalogTourReadModelStoreTestsHelpers.CreateTour(Guid.CreateVersion7(), "Bravo"), TestContext.Current.CancellationToken);
        await sut.UpsertDraft(EfCatalogTourReadModelStoreTestsHelpers.CreateTour(Guid.CreateVersion7(), "Alpha"), TestContext.Current.CancellationToken);

        // Act
        var tours = await sut.ListTours(TestContext.Current.CancellationToken);

        // Assert
        Assert.Collection(
            tours,
            tour => Assert.Equal("Alpha", tour.Title),
            tour => Assert.Equal("Bravo", tour.Title),
            tour => Assert.Equal("Zulu", tour.Title));
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
        Assert.NotNull(tour);
        Assert.Equal(tourId, tour.CatalogTourId);
        Assert.Equal("Draft", tour.Title);
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
        Assert.Null(tour);
    }

    [Fact]
    public async Task UpdatePresentation_updates_public_fields_and_enables_slug_lookup()
    {
        // Arrange
        await using var dbContext = EfPublicContentStoreTestDbContextFactory.Create();
        var sut = new EfCatalogTourReadModelStore(dbContext);
        var tourId = Guid.CreateVersion7();
        await sut.UpsertDraft(EfCatalogTourReadModelStoreTestsHelpers.CreateTour(tourId, "Draft"), TestContext.Current.CancellationToken);

        // Act
        var updated = await sut.UpdatePresentation(
            tourId,
            new CatalogTourPresentationUpdate("Published Tour", "published-tour", IsPublished: true),
            TestContext.Current.CancellationToken);
        var published = await sut.GetPublishedTourBySlug("published-tour", TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(updated);
        Assert.Equal("Published Tour", updated.Title);
        Assert.Equal("published-tour", updated.Slug);
        Assert.True(updated.IsPublished);
        Assert.NotNull(published);
        Assert.Equal(tourId, published.CatalogTourId);
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
            new CatalogTourPresentationUpdate("  Public Title  ", "  Mixed-Case-Slug  ", IsPublished: true),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(updated);
        Assert.Equal("Public Title", updated.Title);
        Assert.Equal("Mixed-Case-Slug", updated.Slug);
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
            new CatalogTourPresentationUpdate("Public Title", "public-title", IsPublished: true),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(updated);
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
        Assert.NotNull(tour);
        Assert.Equal("mixed-slug", tour.Slug);
    }

    [Fact]
    public async Task GetPublishedTourBySlug_trims_lookup_slug()
    {
        // Arrange
        await using var dbContext = EfPublicContentStoreTestDbContextFactory.Create();
        var sut = new EfCatalogTourReadModelStore(dbContext);
        var tourId = Guid.CreateVersion7();
        await sut.UpsertDraft(
            EfCatalogTourReadModelStoreTestsHelpers.CreateTour(tourId, "Draft", "mixed-slug", isPublished: true),
            TestContext.Current.CancellationToken);

        // Act
        var tour = await sut.GetPublishedTourBySlug(" mixed-slug ", TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(tour);
        Assert.Equal(tourId, tour.CatalogTourId);
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
        Assert.Null(tour);
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
        Assert.Null(tour);
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
            new CatalogTourPresentationUpdate("Public Title", "public-title", IsPublished: true),
            TestContext.Current.CancellationToken);

        // Act
        await sut.UpsertDraft(EfCatalogTourReadModelStoreTestsHelpers.CreateTour(tourId, "Projection Title"), TestContext.Current.CancellationToken);
        var tours = await sut.ListTours(TestContext.Current.CancellationToken);

        // Assert
        var tour = Assert.Single(tours);
        Assert.Equal("Public Title", tour.Title);
        Assert.Equal("public-title", tour.Slug);
        Assert.True(tour.IsPublished);
    }

}
