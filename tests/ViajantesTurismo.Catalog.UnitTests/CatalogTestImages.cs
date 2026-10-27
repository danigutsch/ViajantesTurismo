using ImageMagick;

namespace ViajantesTurismo.Catalog.UnitTests;

/// <summary>Test image builders.</summary>
internal static class CatalogTestImages
{
    /// <summary>Creates a solid blue JPEG of the given pixel size.</summary>
    public static byte[] CreateJpeg(uint width, uint height)
    {
        using var image = new MagickImage(MagickColors.Blue, width, height);
        image.Format = MagickFormat.Jpeg;

        return image.ToByteArray();
    }
}
