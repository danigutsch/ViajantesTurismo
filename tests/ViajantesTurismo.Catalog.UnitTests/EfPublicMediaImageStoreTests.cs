using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class EfPublicMediaImageStoreTests
{
    [Fact]
    public async Task Store_persists_and_loads_ordered_tour_gallery_images()
    {
        // Arrange
        await using var dbContext = EfPublicContentStoreTestDbContextFactory.Create();
        var store = new EfPublicMediaImageStore(dbContext);
        var tourId = Guid.CreateVersion7();
        var regular = PublicMediaImageTestFactory.CreateImage(tourId, displayOrder: 1, isCover: false);
        var cover = PublicMediaImageTestFactory.CreateImage(tourId, displayOrder: 2, isCover: true);

        // Act
        await store.Upsert(regular, TestContext.Current.CancellationToken);
        await store.Upsert(cover, TestContext.Current.CancellationToken);
        var images = await store.ListByTour(tourId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Collection(
            images,
            image =>
            {
                Assert.Equal(cover.Id, image.Id);
                Assert.Single(image.TourLinks);
            },
            image => Assert.Equal(regular.Id, image.Id));
    }

    [Fact]
    public async Task Store_replaces_existing_image_metadata()
    {
        // Arrange
        await using var dbContext = EfPublicContentStoreTestDbContextFactory.Create();
        var store = new EfPublicMediaImageStore(dbContext);
        var imageId = Guid.CreateVersion7();
        var tourId = Guid.CreateVersion7();
        var original = PublicMediaImageTestFactory.CreateImage(tourId, imageId, displayOrder: 1, isCover: false);
        var replacement = PublicMediaImageTestFactory.CreateImage(tourId, imageId, displayOrder: 0, isCover: true, altText: "Updated image");

        // Act
        await store.Upsert(original, TestContext.Current.CancellationToken);
        await store.Upsert(replacement, TestContext.Current.CancellationToken);
        var saved = await store.GetImage(imageId, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(saved);
        Assert.Equal("Updated image", saved.AltText);
        var link = Assert.Single(saved.TourLinks);
        Assert.True(link.IsCover);
        Assert.Equal(0, link.DisplayOrder);
    }
}
