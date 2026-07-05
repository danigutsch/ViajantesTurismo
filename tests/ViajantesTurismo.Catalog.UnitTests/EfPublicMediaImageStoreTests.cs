using SharedKernel.Testing.Assertions;
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
        images.Count.ShouldBe(2);
        images[0].Id.ShouldBe(cover.Id);
        images[0].TourLinks.Count.ShouldBe(1);
        images[1].Id.ShouldBe(regular.Id);
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
        saved.ShouldNotBeNull();
        saved.AltText.ShouldBe("Updated image");
        var link = saved.TourLinks.ShouldHaveSingleItem();
        link.IsCover.ShouldBeTrue();
        link.DisplayOrder.ShouldBe(0);
    }

    [Fact]
    public async Task Store_persists_multiple_responsive_variants()
    {
        // Arrange
        await using var dbContext = EfPublicContentStoreTestDbContextFactory.Create();
        var store = new EfPublicMediaImageStore(dbContext);
        var tourId = Guid.CreateVersion7();
        var imageId = Guid.CreateVersion7();
        var image = PublicMediaImageTestFactory.CreateImageWithVariants(
            tourId,
            imageId,
            MediaImageProcessingStatus.Ready,
            [
                new MediaImageResponsiveVariant("media/one-320.jpg", 320, 213, "image/jpeg", 512, 0),
                new MediaImageResponsiveVariant("media/one-640.jpg", 640, 427, "image/jpeg", 1024, 1)
            ]);

        // Act
        await store.Upsert(image, TestContext.Current.CancellationToken);
        var saved = await store.GetImage(imageId, TestContext.Current.CancellationToken);

        // Assert
        saved.ShouldNotBeNull();
        saved.ResponsiveVariants.Count.ShouldBe(2);
        saved.ResponsiveVariants[0].Width.ShouldBe(320);
        saved.ResponsiveVariants[1].Width.ShouldBe(640);
    }

    [Fact]
    public async Task Store_lists_distinct_referenced_object_keys()
    {
        // Arrange
        await using var dbContext = EfPublicContentStoreTestDbContextFactory.Create();
        var store = new EfPublicMediaImageStore(dbContext);
        var image = PublicMediaImageTestFactory.CreateImageWithVariants(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            MediaImageProcessingStatus.Ready,
            [
                new MediaImageResponsiveVariant("media/one-320.jpg", 320, 213, "image/jpeg", 512, 0),
                new MediaImageResponsiveVariant("media/one-640.jpg", 640, 427, "image/jpeg", 1024, 1)
            ]);

        // Act
        await store.Upsert(image, TestContext.Current.CancellationToken);
        var keys = await store.ListReferencedObjectKeys(TestContext.Current.CancellationToken);

        // Assert
        keys.ShouldBe([
            "media/one-320.jpg",
            "media/one-640.jpg",
            "media/source.jpg"
        ]);
    }
}
