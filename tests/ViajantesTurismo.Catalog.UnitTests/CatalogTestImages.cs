using ImageMagick;

namespace ViajantesTurismo.Catalog.UnitTests;

internal static class CatalogTestImages
{
    public static byte[] CreateJpeg(uint width, uint height)
    {
        using var image = new MagickImage(MagickColors.Blue, width, height);
        image.Format = MagickFormat.Jpeg;

        return image.ToByteArray();
    }
}
