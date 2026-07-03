using SharedKernel.Testing.Assertions;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Domain.Media;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class MediaImageProcessingRequestedIntegrationEventConsumerTests
{
    [Fact]
    public async Task Handle_processes_original_into_deterministic_public_variants()
    {
        // Arrange
        var mediaImageId = Guid.CreateVersion7();
        var content = CatalogTestImages.CreateJpeg(64, 32);
        var objectStore = new InMemoryMediaObjectStore();
        await objectStore.Put(
            new MediaObjectWriteRequest("uploads/original.jpg", new MemoryStream(content), "image/jpeg", content.Length),
            TestContext.Current.CancellationToken);
        var imageStore = new InMemoryPublicMediaImageStore(PublicMediaImageTestFactory.CreatePendingImage(mediaImageId, content.Length));
        var consumer = new MediaImageProcessingRequestedIntegrationEventConsumer(objectStore, imageStore);
        var notification = new MediaImageProcessingRequestedIntegrationEvent(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            mediaImageId,
            "uploads/original.jpg",
            1);

        // Act
        await consumer.Handle(notification, TestContext.Current.CancellationToken);
        await consumer.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        imageStore.Current.ProcessingStatus.ShouldBe(MediaImageProcessingStatus.Ready);
        imageStore.Current.ResponsiveVariants.Count.ShouldBe(15);
        objectStore.ObjectKeys.Count.ShouldBe(18);
        objectStore.ObjectKeys.ShouldContain($"media/{mediaImageId:N}/v1/320-avif.avif");
        objectStore.ObjectKeys.ShouldContain($"media/{mediaImageId:N}/v1/1920-jpeg.jpg");
        objectStore.ObjectKeys.ShouldContain($"media/{mediaImageId:N}/v1/thumb-webp.webp");
        objectStore.ObjectKeys.ShouldContain($"media/{mediaImageId:N}/v1/icon-ico.ico");
    }

    [Fact]
    public async Task Handle_marks_image_failed_when_the_original_cannot_be_decoded()
    {
        // Arrange
        var mediaImageId = Guid.CreateVersion7();
        var objectStore = new InMemoryMediaObjectStore();
        await objectStore.Put(
            new MediaObjectWriteRequest("uploads/not-image.bin", new MemoryStream([0x01, 0x02, 0x03]), "application/octet-stream", 3),
            TestContext.Current.CancellationToken);
        var imageStore = new InMemoryPublicMediaImageStore(PublicMediaImageTestFactory.CreatePendingImage(mediaImageId, 3));
        var consumer = new MediaImageProcessingRequestedIntegrationEventConsumer(objectStore, imageStore);

        // Act
        await consumer.Handle(
            new MediaImageProcessingRequestedIntegrationEvent(
                Guid.CreateVersion7(),
                DateTimeOffset.UtcNow,
                mediaImageId,
                "uploads/not-image.bin",
                1),
            TestContext.Current.CancellationToken);

        // Assert
        imageStore.Current.ProcessingStatus.ShouldBe(MediaImageProcessingStatus.Failed);
        imageStore.Current.ResponsiveVariants.Count.ShouldBe(0);
    }
}
