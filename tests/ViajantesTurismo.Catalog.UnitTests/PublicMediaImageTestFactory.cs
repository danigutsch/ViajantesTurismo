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
}
