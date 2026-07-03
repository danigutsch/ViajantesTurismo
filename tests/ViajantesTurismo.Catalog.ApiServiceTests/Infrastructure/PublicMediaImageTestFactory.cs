using ViajantesTurismo.Catalog.Domain.Media;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure;

internal static class PublicMediaImageTestFactory
{
    public static PublicMediaImage CreateReadyImage(
        Guid tourId,
        string sourceName,
        string variantName,
        string checksum,
        string altText,
        int displayOrder,
        bool isCover)
    {
        var result = PublicMediaImage.Create(
            new PublicMediaImageMetadata
            {
                Id = Guid.CreateVersion7(),
                SourceUri = new Uri($"https://private.example/{sourceName}"),
                Checksum = checksum,
                ContentType = "image/jpeg",
                FileSizeBytes = 2048,
                Dimensions = new MediaImageDimensions(1200, 800),
                ProcessingStatus = MediaImageProcessingStatus.Ready,
                AltText = altText,
            },
            [new MediaImageResponsiveVariant(new Uri($"https://cdn.example/{variantName}"), 640, 427, "image/jpeg", 1024)],
            ["catalog"],
            [new MediaImageTourLink(tourId, displayOrder, isCover)]);

        return result.Value;
    }

    public static PublicMediaImage CreateFailedImage(Guid tourId)
    {
        var result = PublicMediaImage.Create(
            new PublicMediaImageMetadata
            {
                Id = Guid.CreateVersion7(),
                SourceUri = new Uri("https://private.example/failed-source.jpg"),
                Checksum = "sha256:ghi",
                ContentType = "image/jpeg",
                FileSizeBytes = 2048,
                Dimensions = new MediaImageDimensions(1200, 800),
                ProcessingStatus = MediaImageProcessingStatus.Failed,
                AltText = "Failed image",
            },
            [new MediaImageResponsiveVariant(new Uri("https://cdn.example/failed-640.jpg"), 640, 427, "image/jpeg", 1024)],
            ["failed"],
            [new MediaImageTourLink(tourId, 2, false)]);

        return result.Value;
    }
}
