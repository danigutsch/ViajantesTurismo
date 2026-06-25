using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class InMemoryCatalogTourReadModelStoreTests
{
    [Fact]
    public async Task ListTours_Returns_Stable_Title_Then_Id_Ordering()
    {
        // Arrange
        var sut = new InMemoryCatalogTourReadModelStore();
        var alphaLaterId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var alphaEarlierId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        await sut.UpsertDraft(InMemoryCatalogTourReadModelStoreTestsHelpers.CreateTour(Guid.CreateVersion7(), "Zulu"), TestContext.Current.CancellationToken);
        await sut.UpsertDraft(InMemoryCatalogTourReadModelStoreTestsHelpers.CreateTour(alphaLaterId, "alpha"), TestContext.Current.CancellationToken);
        await sut.UpsertDraft(InMemoryCatalogTourReadModelStoreTestsHelpers.CreateTour(alphaEarlierId, "Alpha"), TestContext.Current.CancellationToken);

        // Act
        var tours = await sut.ListTours(TestContext.Current.CancellationToken);

        // Assert
        Assert.Collection(
            tours,
            tour => Assert.Equal(alphaEarlierId, tour.CatalogTourId),
            tour => Assert.Equal(alphaLaterId, tour.CatalogTourId),
            tour => Assert.Equal("Zulu", tour.Title));
    }

    [Fact]
    public async Task UpsertDraft_Replaces_Existing_Tour_By_Catalog_Id()
    {
        // Arrange
        var sut = new InMemoryCatalogTourReadModelStore();
        var tourId = Guid.CreateVersion7();
        await sut.UpsertDraft(InMemoryCatalogTourReadModelStoreTestsHelpers.CreateTour(tourId, "Original"), TestContext.Current.CancellationToken);

        // Act
        await sut.UpsertDraft(InMemoryCatalogTourReadModelStoreTestsHelpers.CreateTour(tourId, "Updated"), TestContext.Current.CancellationToken);
        var tours = await sut.ListTours(TestContext.Current.CancellationToken);

        // Assert
        var tour = Assert.Single(tours);
        Assert.Equal("Updated", tour.Title);
    }

    [Fact]
    public async Task GetPublishedTourBySlug_Returns_Null_Until_Published_Read_Model_Exists()
    {
        // Arrange
        var sut = new InMemoryCatalogTourReadModelStore();
        await sut.UpsertDraft(InMemoryCatalogTourReadModelStoreTestsHelpers.CreateTour(Guid.CreateVersion7(), "Draft"), TestContext.Current.CancellationToken);

        // Act
        var tour = await sut.GetPublishedTourBySlug("draft", TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(tour);
    }

    [Fact]
    public async Task Store_Methods_Honor_Cancellation()
    {
        // Arrange
        var sut = new InMemoryCatalogTourReadModelStore();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var upsert = Assert.ThrowsAsync<OperationCanceledException>(
            async () => await sut.UpsertDraft(InMemoryCatalogTourReadModelStoreTestsHelpers.CreateTour(Guid.CreateVersion7(), "Cancelled"), cts.Token));
        var list = Assert.ThrowsAsync<OperationCanceledException>(async () => await sut.ListTours(cts.Token));
        var lookup = Assert.ThrowsAsync<OperationCanceledException>(
            async () => await sut.GetPublishedTourBySlug("cancelled", cts.Token));

        // Assert
        await upsert;
        await list;
        await lookup;
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetPublishedTourBySlug_Rejects_Invalid_Slugs(string slug)
    {
        // Arrange
        var sut = new InMemoryCatalogTourReadModelStore();

        // Act
        var lookup = Assert.ThrowsAnyAsync<ArgumentException>(
            async () => await sut.GetPublishedTourBySlug(slug, TestContext.Current.CancellationToken));

        // Assert
        await lookup;
    }

    internal static class InMemoryCatalogTourReadModelStoreTestsHelpers
    {
        public static CatalogTourDraftReadModel CreateTour(Guid id, string title)
        {
            return new CatalogTourDraftReadModel(
                id,
                Guid.CreateVersion7(),
                $"TOUR-{title}",
                title,
                1,
                DateTimeOffset.UtcNow);
        }
    }
}
