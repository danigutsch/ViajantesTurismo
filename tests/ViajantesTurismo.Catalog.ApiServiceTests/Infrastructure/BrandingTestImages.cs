using ImageMagick;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure;

/// <summary>Builds real image bytes for Branding logo upload tests.</summary>
internal static class BrandingTestImages
{
    /// <summary>Creates a solid-color image of the given format and pixel size.</summary>
    public static byte[] Create(MagickFormat format, uint width, uint height)
    {
        using var image = new MagickImage(MagickColors.SeaGreen, width, height);
        image.Format = format;
        return image.ToByteArray();
    }
}
