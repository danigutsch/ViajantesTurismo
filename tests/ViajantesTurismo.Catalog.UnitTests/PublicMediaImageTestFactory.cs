using ViajantesTurismo.Catalog.Domain.Media;

namespace ViajantesTurismo.Catalog.UnitTests;

internal static class PublicMediaImageTestFactory
{
    public static PublicMediaImage CreateImage(
        Guid tourId,
        int displayOrder,
        bool isCover,
        Guid? imageId = null,
        string altText = "Cyclists in the mountains")
    {
        return CreateImage(tourId, imageId ?? Guid.CreateVersion7(), displayOrder, isCover, altText);
    }

    public static PublicMediaImage CreateImage(
        Guid tourId,
        Guid imageId,
        int displayOrder,
        bool isCover,
        string altText = "Cyclists in the mountains")
    {
        return new PublicMediaImage(
            new PublicMediaImageMetadata
            {
                Id = imageId,
                SourceUri = new Uri("https://cdn.example/source.jpg"),
                Checksum = "sha256:abc",
                ContentType = "image/jpeg",
                FileSizeBytes = 2048,
                Dimensions = new MediaImageDimensions(1200, 800),
                ProcessingStatus = MediaImageProcessingStatus.Ready,
                AltText = altText
            },
            [new MediaImageResponsiveVariant(new Uri("https://cdn.example/one-640.jpg"), 640, 427, "image/jpeg", 1024)],
            ["mountain"],
            [new MediaImageTourLink(tourId, displayOrder, isCover)]);
    }

    public static PublicMediaImage CreatePendingImage(Guid imageId, long fileSizeBytes)
    {
        return new PublicMediaImage(
            new PublicMediaImageMetadata
            {
                Id = imageId,
                SourceUri = new Uri("https://cdn.example/original.png"),
                Checksum = "sha256:abc",
                ContentType = "image/png",
                FileSizeBytes = fileSizeBytes,
                Dimensions = new MediaImageDimensions(1, 1),
                ProcessingStatus = MediaImageProcessingStatus.Pending,
                AltText = "Test image"
            },
            [],
            ["test"],
            [new MediaImageTourLink(Guid.CreateVersion7(), 0, true)]);
    }

    public static PublicMediaImage CreateReadyImageWithoutVariants(Guid imageId)
    {
        return CreateImageWithVariants(imageId, MediaImageProcessingStatus.Ready, []);
    }

    public static PublicMediaImage CreateReadyImageForProcessingVersion(Guid imageId, int processingVersion)
    {
        return CreateImageWithVariants(
            imageId,
            MediaImageProcessingStatus.Ready,
            [
                new MediaImageResponsiveVariant(
                    new Uri($"https://cdn.example/media/{imageId:N}/v{processingVersion}/640-jpeg.jpg"),
                    640,
                    427,
                    "image/jpeg",
                    1024)
            ]);
    }

    private static PublicMediaImage CreateImageWithVariants(
        Guid imageId,
        MediaImageProcessingStatus processingStatus,
        IReadOnlyList<MediaImageResponsiveVariant> variants)
    {
        return new PublicMediaImage(
            new PublicMediaImageMetadata
            {
                Id = imageId,
                SourceUri = new Uri("https://cdn.example/source.jpg"),
                Checksum = "sha256:abc",
                ContentType = "image/jpeg",
                FileSizeBytes = 2048,
                Dimensions = new MediaImageDimensions(1200, 800),
                ProcessingStatus = processingStatus,
                AltText = "Cyclists in the mountains"
            },
            variants,
            ["mountain"],
            [new MediaImageTourLink(Guid.CreateVersion7(), 0, true)]);
    }
}
