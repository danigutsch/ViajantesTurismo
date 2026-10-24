using PandaBear.Testing;
using Microsoft.EntityFrameworkCore.Storage;
using ViajantesTurismo.Catalog.Domain.Media;
using ViajantesTurismo.Catalog.Domain.PublicContent;
using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.Catalog.UnitTests;

[Trait(TestTraitNames.AreaName, TestTraits.CatalogArea)]
[Trait(SharedKernelTestTraitNames.ScopeName, SharedKernelTestTraitNames.UnitScope)]
[Trait(SharedKernelTestTraitNames.CapabilityName, TestTraits.AiAccessibilityCapability)]
public sealed class EfPublicMediaImageStoreTests
{
    [Fact]
    public async Task Image_reload_observes_metadata_committed_while_the_caller_waited_for_a_lease()
    {
        // Arrange
        var database = Guid.CreateVersion7().ToString("N");
        var root = new InMemoryDatabaseRoot();
        await using var firstContext = CatalogDbContextTestFactory.Create(database, root);
        await using var secondContext = CatalogDbContextTestFactory.Create(database, root);
        var firstStore = new EfPublicMediaImageStore(firstContext);
        var secondStore = new EfPublicMediaImageStore(secondContext);
        var image = PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 0, true);
        await firstStore.Upsert(image, TestContext.Current.CancellationToken);
        var beforeWait = await firstStore.GetImage(image.Id, TestContext.Current.CancellationToken);
        beforeWait.ShouldNotBeNull();
        var concurrent = await secondStore.GetImage(image.Id, TestContext.Current.CancellationToken);
        concurrent.ShouldNotBeNull();
        concurrent.SetReviewedAccessibilityText(PublicContentLanguage.EnUs, "Publisher approved replacement", "Approved caption", false).IsSuccess.ShouldBeTrue();
        await secondStore.Upsert(concurrent, TestContext.Current.CancellationToken);

        // Act
        var afterWait = await firstStore.GetImage(image.Id, TestContext.Current.CancellationToken);

        // Assert
        afterWait.ShouldNotBeNull();
        afterWait.AltText.ShouldBe("Publisher approved replacement");
        afterWait.Caption.ShouldBe("Approved caption");
    }

    [Fact]
    public async Task Pending_draft_timestamp_survives_persistence_and_clears_only_for_reviewed_language()
    {
        // Arrange
        await using var dbContext = EfPublicContentStoreTestDbContextFactory.Create();
        var store = new EfPublicMediaImageStore(dbContext);
        var image = PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), Guid.CreateVersion7(), 0, true);
        var firstDraftAt = new DateTimeOffset(2026, 10, 21, 10, 0, 0, TimeSpan.Zero);
        var regeneratedAt = firstDraftAt.AddHours(1);
        image.SetAiDraftAccessibilityText(PublicContentLanguage.EnUs, "A beach", null, firstDraftAt).IsSuccess.ShouldBeTrue();
        image.SetAiDraftAccessibilityText(PublicContentLanguage.PtBr, "Uma praia", null, firstDraftAt).IsSuccess.ShouldBeTrue();
        image.SetAiDraftAccessibilityText(PublicContentLanguage.EnUs, "A blue sea", null, regeneratedAt).IsSuccess.ShouldBeTrue();
        await store.Upsert(image, TestContext.Current.CancellationToken);
        dbContext.ChangeTracker.Clear();
        var pending = await store.GetImage(image.Id, TestContext.Current.CancellationToken);
        pending.ShouldNotBeNull();
        pending.AccessibilityTexts.ShouldHaveSingleItem(text => text.Language == PublicContentLanguage.EnUs).PendingDraftCreatedAt.ShouldBe(regeneratedAt);
        pending.AccessibilityTexts.ShouldHaveSingleItem(text => text.Language == PublicContentLanguage.PtBr).PendingDraftCreatedAt.ShouldBe(firstDraftAt);

        // Act
        pending.SetReviewedAccessibilityText(PublicContentLanguage.EnUs, "A blue sea", null, false).IsSuccess.ShouldBeTrue();
        await store.Upsert(pending, TestContext.Current.CancellationToken);
        dbContext.ChangeTracker.Clear();
        var reviewed = await store.GetImage(image.Id, TestContext.Current.CancellationToken);

        // Assert
        reviewed.ShouldNotBeNull();
        reviewed.AccessibilityTexts.ShouldHaveSingleItem(text => text.Language == PublicContentLanguage.EnUs).PendingDraftCreatedAt.ShouldBeNull();
        reviewed.AccessibilityTexts.ShouldHaveSingleItem(text => text.Language == PublicContentLanguage.PtBr).PendingDraftCreatedAt.ShouldBe(firstDraftAt);
    }

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
    public async Task Store_persists_accessibility_review_state()
    {
        // Arrange
        await using var dbContext = EfPublicContentStoreTestDbContextFactory.Create();
        var store = new EfPublicMediaImageStore(dbContext);
        var image = PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), displayOrder: 0, isCover: true);
        image.SetAiDraftAccessibilityText(PublicContentLanguage.PtBr, "Ciclistas em uma trilha de montanha", "Passeio de bicicleta").IsSuccess.ShouldBeTrue();

        // Act
        await store.Upsert(image, TestContext.Current.CancellationToken);
        var saved = await store.GetImage(image.Id, TestContext.Current.CancellationToken);

        // Assert
        saved.ShouldNotBeNull();
        saved.AccessibilityTexts.Count.ShouldBe(2);
        var localized = saved.AccessibilityTexts.ShouldHaveSingleItem(text => text.Language == PublicContentLanguage.PtBr);
        localized.RequiresHumanReview.ShouldBeTrue();
        localized.IsAiGenerated.ShouldBeTrue();
        localized.AltText.ShouldBe("Ciclistas em uma trilha de montanha");
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
