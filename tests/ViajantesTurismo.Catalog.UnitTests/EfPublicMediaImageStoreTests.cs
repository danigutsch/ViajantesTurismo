using ViajantesTurismo.Catalog.Domain.Media;
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

    [Fact]
    public async Task Store_persists_multiple_responsive_variants()
    {
        // Arrange
        await using var dbContext = EfPublicContentStoreTestDbContextFactory.Create();
        var store = new EfPublicMediaImageStore(dbContext);
        var tourId = Guid.CreateVersion7();
        var imageId = Guid.CreateVersion7();
        var image = new PublicMediaImage(
            new PublicMediaImageMetadata
            {
                Id = imageId,
                SourceUri = new Uri("https://cdn.example/source.jpg"),
                Checksum = "sha256:abc",
                ContentType = "image/jpeg",
                FileSizeBytes = 4096,
                Dimensions = new MediaImageDimensions(1200, 800),
                ProcessingStatus = MediaImageProcessingStatus.Ready,
                AltText = "Cyclists in the mountains"
            },
            [
                new MediaImageResponsiveVariant(new Uri("https://cdn.example/one-320.jpg"), 320, 213, "image/jpeg", 512),
                new MediaImageResponsiveVariant(new Uri("https://cdn.example/one-640.jpg"), 640, 427, "image/jpeg", 1024)
            ],
            ["mountain"],
            [new MediaImageTourLink(tourId, 1, true)]);

        // Act
        await store.Upsert(image, TestContext.Current.CancellationToken);
        var saved = await store.GetImage(imageId, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(saved);
        Assert.Collection(
            saved.ResponsiveVariants,
            variant => Assert.Equal(320, variant.Width),
            variant => Assert.Equal(640, variant.Width));
    }
}
