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
            imageId,
            new Uri("https://cdn.example/source.jpg"),
            "sha256:abc",
            "image/jpeg",
            2048,
            new MediaImageDimensions(1200, 800),
            MediaImageProcessingStatus.Ready,
            [new MediaImageResponsiveVariant(new Uri("https://cdn.example/one-640.jpg"), 640, 427, "image/jpeg", 1024)],
            ["mountain"],
            [new MediaImageTourLink(tourId, displayOrder, isCover)],
            altText,
            Caption: null,
            Attribution: null,
            Copyright: null);
    }
}
