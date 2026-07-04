using System.Globalization;
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
        var result = PublicMediaImage.Create(
            new PublicMediaImageMetadata
            {
                Id = imageId,
                SourceObjectKey = "media/source.jpg",
                Checksum = "sha256:abc",
                ContentType = "image/jpeg",
                FileSizeBytes = 2048,
                Dimensions = new MediaImageDimensions(1200, 800),
                ProcessingStatus = MediaImageProcessingStatus.Ready,
                AltText = altText
            },
            [new MediaImageResponsiveVariant("media/one-640.jpg", 640, 427, "image/jpeg", 1024)],
            ["mountain"],
            [new MediaImageTourLink(tourId, displayOrder, isCover)]);

        return result.Value;
    }

    public static PublicMediaImage CreatePendingImage(Guid imageId, long fileSizeBytes)
    {
        var result = PublicMediaImage.Create(
            new PublicMediaImageMetadata
            {
                Id = imageId,
                SourceObjectKey = "media/original.png",
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

        return result.Value;
    }

    public static PublicMediaImage CreateReadyImageForProcessingVersion(Guid imageId, int processingVersion)
    {
        return CreateReadyImageWithVariantUri(
            imageId,
            new Uri(string.Create(
                CultureInfo.InvariantCulture,
                $"https://cdn.example/media/{imageId:N}/v{processingVersion}/640-jpeg.jpg")));
    }

    public static PublicMediaImage CreateReadyImageWithVariantUri(Guid imageId, Uri variantUri)
    {
        return CreateImageWithVariants(
            Guid.CreateVersion7(),
            imageId,
            MediaImageProcessingStatus.Ready,
            [new MediaImageResponsiveVariant(variantUri.AbsolutePath.TrimStart('/'), 640, 427, "image/jpeg", 1024)]);
    }

    public static PublicMediaImage CreateImageWithVariants(
        Guid tourId,
        Guid imageId,
        MediaImageProcessingStatus processingStatus,
        IReadOnlyList<MediaImageResponsiveVariant> variants)
    {
        var result = PublicMediaImage.Create(
            new PublicMediaImageMetadata
            {
                Id = imageId,
                SourceObjectKey = "media/source.jpg",
                Checksum = "sha256:abc",
                ContentType = "image/jpeg",
                FileSizeBytes = 2048,
                Dimensions = new MediaImageDimensions(1200, 800),
                ProcessingStatus = processingStatus,
                AltText = "Cyclists in the mountains"
            },
            variants,
            ["mountain"],
            [new MediaImageTourLink(tourId, 0, true)]);

        return result.Value;
    }
}
