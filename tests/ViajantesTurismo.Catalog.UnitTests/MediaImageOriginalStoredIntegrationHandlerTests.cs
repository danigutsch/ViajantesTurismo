using PandaBear.ObjectStorage;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Contracts.IntegrationEvents.Media;
using ViajantesTurismo.Catalog.Domain.Media;
using ViajantesTurismo.Catalog.Domain.PublicContent;
using ViajantesTurismo.Catalog.Testing.Infrastructure;
using PandaBear.Testing;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class MediaImageOriginalStoredIntegrationHandlerTests
{
    [Fact]
    [Trait(TestTraitNames.CategoryName, TestTraitValues.SecurityCategory)]
    public async Task Processing_reloads_reviewed_metadata_after_waiting_for_a_gallery_edit()
    {
        // Arrange
        var imageId = Guid.CreateVersion7();
        var content = CatalogTestImages.CreateJpeg(320, 160);
        var original = PublicMediaImageTestFactory.CreatePendingImage(imageId, content.Length);
        var objects = new InMemoryMediaObjectStore();
        await objects.Put(new ObjectWriteRequest("media/original.png", new MemoryStream(content), "image/jpeg", content.Length), TestContext.Current.CancellationToken);
        var images = new InMemoryPublicMediaImageStore(original);
        var mutationLock = new TestCatalogTourMutationLock();
        var observed = new ObservedCatalogTourMutationLock(mutationLock);
        await using var held = await mutationLock.Acquire(original.TourLinks.Select(link => link.CatalogTourId).ToArray(), TestContext.Current.CancellationToken);
        var handler = new MediaImageOriginalStoredIntegrationHandler(objects, images, observed);
        var notification = new MediaImageOriginalStoredIntegrationEvent(Guid.CreateVersion7(), TimeProvider.System.GetUtcNow(), imageId, "media/original.png", 1);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));

        // Act
        var processing = handler.Handle(notification, timeout.Token).AsTask();
        await observed.AcquisitionStarted.WaitAsync(timeout.Token);
        var replacementResult = PublicMediaImage.Create(
            new PublicMediaImageMetadata
            {
                Id = imageId,
                SourceObjectKey = original.SourceObjectKey,
                Checksum = original.Checksum,
                ContentType = original.ContentType,
                FileSizeBytes = original.FileSizeBytes,
                Dimensions = original.Dimensions,
                ProcessingStatus = original.ProcessingStatus,
                AltText = "Reviewed replacement"
            }, original.ResponsiveVariants, original.Tags, original.TourLinks);
        replacementResult.IsSuccess.ShouldBeTrue();
        var replacement = replacementResult.Value;
        replacement.SetReviewedAccessibilityText(PublicContentLanguage.EnUs, "Reviewed replacement", "Reviewed caption", false).IsSuccess.ShouldBeTrue();
        await images.Upsert(replacement, timeout.Token);

        // Assert
        processing.IsCompleted.ShouldBeFalse();

        // Act
        await held.DisposeAsync();
        await processing;

        // Assert
        images.Current.ProcessingStatus.ShouldBe(MediaImageProcessingStatus.Ready);
        images.Current.AltText.ShouldBe("Reviewed replacement");
        images.Current.Caption.ShouldBe("Reviewed caption");
        images.Current.RequiresHumanReview.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_processes_original_into_deterministic_public_variants()
    {
        // Arrange
        var mediaImageId = Guid.CreateVersion7();
        var content = CatalogTestImages.CreateJpeg(640, 320);
        var objectStore = new InMemoryMediaObjectStore();
        await objectStore.Put(
            new ObjectWriteRequest("uploads/original.jpg", new MemoryStream(content), "image/jpeg", content.Length),
            TestContext.Current.CancellationToken);
        var imageStore = new InMemoryPublicMediaImageStore(PublicMediaImageTestFactory.CreatePendingImage(mediaImageId, content.Length));
        var handler = new MediaImageOriginalStoredIntegrationHandler(objectStore, imageStore, new TestCatalogTourMutationLock());
        var notification = new MediaImageOriginalStoredIntegrationEvent(
            Guid.CreateVersion7(),
            TimeProvider.System.GetUtcNow(),
            mediaImageId,
            "uploads/original.jpg",
            1);

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        imageStore.Current.ProcessingStatus.ShouldBe(MediaImageProcessingStatus.Ready);
        imageStore.Current.ResponsiveVariants.Count.ShouldBe(6);
        objectStore.ObjectKeys.Count.ShouldBe(9);
        objectStore.ObjectKeys.ShouldContain($"media/{mediaImageId:N}/v1/320-avif.avif");
        objectStore.ObjectKeys.ShouldContain($"media/{mediaImageId:N}/v1/640-jpeg.jpg");
        objectStore.ObjectKeys.ShouldNotContain($"media/{mediaImageId:N}/v1/960-jpeg.jpg");
        objectStore.ObjectKeys.ShouldContain($"media/{mediaImageId:N}/v1/thumb-webp.webp");
        objectStore.ObjectKeys.ShouldContain($"media/{mediaImageId:N}/v1/icon-ico.ico");
    }

    [Fact]
    public async Task Handle_reprocesses_ready_image_when_processing_version_changes()
    {
        // Arrange
        var mediaImageId = Guid.CreateVersion7();
        var content = CatalogTestImages.CreateJpeg(640, 320);
        var objectStore = new InMemoryMediaObjectStore();
        await objectStore.Put(
            new ObjectWriteRequest("uploads/original.jpg", new MemoryStream(content), "image/jpeg", content.Length),
            TestContext.Current.CancellationToken);
        var imageStore = new InMemoryPublicMediaImageStore(PublicMediaImageTestFactory.CreatePendingImage(mediaImageId, content.Length));
        var handler = new MediaImageOriginalStoredIntegrationHandler(objectStore, imageStore, new TestCatalogTourMutationLock());

        // Act
        await handler.Handle(
            new MediaImageOriginalStoredIntegrationEvent(
                Guid.CreateVersion7(),
                TimeProvider.System.GetUtcNow(),
                mediaImageId,
                "uploads/original.jpg",
                1),
            TestContext.Current.CancellationToken);
        await handler.Handle(
            new MediaImageOriginalStoredIntegrationEvent(
                Guid.CreateVersion7(),
                TimeProvider.System.GetUtcNow(),
                mediaImageId,
                "uploads/original.jpg",
                2),
            TestContext.Current.CancellationToken);

        // Assert
        objectStore.ObjectKeys.ShouldContain($"media/{mediaImageId:N}/v1/640-jpeg.jpg");
        objectStore.ObjectKeys.ShouldContain($"media/{mediaImageId:N}/v2/640-jpeg.jpg");
        imageStore.Current.ResponsiveVariants.ShouldAllSatisfy(
            variant => variant.ObjectKey.Contains("/v2/", StringComparison.Ordinal).ShouldBe(true));
    }

    [Fact]
    public async Task Handle_keeps_ready_variants_when_reprocessing_fails()
    {
        // Arrange
        var mediaImageId = Guid.CreateVersion7();
        var objectStore = new InMemoryMediaObjectStore();
        await objectStore.Put(
            new ObjectWriteRequest("uploads/not-image.bin", new MemoryStream([0x01, 0x02, 0x03]), "application/octet-stream", 3),
            TestContext.Current.CancellationToken);
        var existingImage = PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), mediaImageId, 0, true);
        var existingVariant = existingImage.ResponsiveVariants.ShouldHaveSingleItem();
        var imageStore = new InMemoryPublicMediaImageStore(existingImage);
        var handler = new MediaImageOriginalStoredIntegrationHandler(objectStore, imageStore, new TestCatalogTourMutationLock());

        // Act
        await handler.Handle(
            new MediaImageOriginalStoredIntegrationEvent(
                Guid.CreateVersion7(),
                TimeProvider.System.GetUtcNow(),
                mediaImageId,
                "uploads/not-image.bin",
                2),
            TestContext.Current.CancellationToken);

        // Assert
        imageStore.Current.ProcessingStatus.ShouldBe(MediaImageProcessingStatus.Ready);
        var currentVariant = imageStore.Current.ResponsiveVariants.ShouldHaveSingleItem();
        currentVariant.ObjectKey.ShouldBe(existingVariant.ObjectKey);
    }

    [Fact]
    public async Task Handle_marks_image_failed_when_the_original_cannot_be_decoded()
    {
        // Arrange
        var mediaImageId = Guid.CreateVersion7();
        var objectStore = new InMemoryMediaObjectStore();
        await objectStore.Put(
            new ObjectWriteRequest("uploads/not-image.bin", new MemoryStream([0x01, 0x02, 0x03]), "application/octet-stream", 3),
            TestContext.Current.CancellationToken);
        var imageStore = new InMemoryPublicMediaImageStore(PublicMediaImageTestFactory.CreatePendingImage(mediaImageId, 3));
        var handler = new MediaImageOriginalStoredIntegrationHandler(objectStore, imageStore, new TestCatalogTourMutationLock());

        // Act
        await handler.Handle(
            new MediaImageOriginalStoredIntegrationEvent(
                Guid.CreateVersion7(),
                TimeProvider.System.GetUtcNow(),
                mediaImageId,
                "uploads/not-image.bin",
                1),
            TestContext.Current.CancellationToken);

        // Assert
        imageStore.Current.ProcessingStatus.ShouldBe(MediaImageProcessingStatus.Failed);
        imageStore.Current.ResponsiveVariants.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_throws_with_media_image_id_when_processed_ready_image_has_no_public_variants()
    {
        // Arrange
        var mediaImageId = Guid.CreateVersion7();
        var content = CatalogTestImages.CreateJpeg(100, 50);
        var objectStore = new InMemoryMediaObjectStore();
        await objectStore.Put(
            new ObjectWriteRequest("uploads/tiny-original.jpg", new MemoryStream(content), "image/jpeg", content.Length),
            TestContext.Current.CancellationToken);
        var imageStore = new InMemoryPublicMediaImageStore(PublicMediaImageTestFactory.CreatePendingImage(mediaImageId, content.Length));
        var handler = new MediaImageOriginalStoredIntegrationHandler(objectStore, imageStore, new TestCatalogTourMutationLock());
        var notification = new MediaImageOriginalStoredIntegrationEvent(
            Guid.CreateVersion7(),
            TimeProvider.System.GetUtcNow(),
            mediaImageId,
            "uploads/tiny-original.jpg",
            1);

        // Act
        Func<Task> action = async () => await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        var exception = await action.ShouldThrow<InvalidOperationException>();
        exception.Message.ShouldContain(mediaImageId.ToString(), StringComparison.Ordinal);
        exception.Message.ShouldContain("Processed media image", StringComparison.Ordinal);
        imageStore.Current.ProcessingStatus.ShouldBe(MediaImageProcessingStatus.Pending);
        var remainingObjectKey = objectStore.ObjectKeys.ShouldHaveSingleItem();
        remainingObjectKey.ShouldBe("uploads/tiny-original.jpg");
    }

    [Fact]
    public async Task Handle_deletes_stored_variants_when_metadata_persistence_fails()
    {
        // Arrange
        var mediaImageId = Guid.CreateVersion7();
        var content = CatalogTestImages.CreateJpeg(640, 320);
        var objectStore = new InMemoryMediaObjectStore();
        await objectStore.Put(
            new ObjectWriteRequest("uploads/original.jpg", new MemoryStream(content), "image/jpeg", content.Length),
            TestContext.Current.CancellationToken);
        var imageStore = new ThrowingPublicMediaImageStore(
            PublicMediaImageTestFactory.CreatePendingImage(mediaImageId, content.Length),
            new InvalidOperationException("database unavailable"));
        var handler = new MediaImageOriginalStoredIntegrationHandler(objectStore, imageStore, new TestCatalogTourMutationLock());
        var notification = new MediaImageOriginalStoredIntegrationEvent(
            Guid.CreateVersion7(),
            TimeProvider.System.GetUtcNow(),
            mediaImageId,
            "uploads/original.jpg",
            1);

        // Act
        Func<Task> action = async () => await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        await action.ShouldThrow<InvalidOperationException>();
        var remainingObjectKey = objectStore.ObjectKeys.ShouldHaveSingleItem();
        remainingObjectKey.ShouldBe("uploads/original.jpg");
    }
}
