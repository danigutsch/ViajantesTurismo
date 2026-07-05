using ImageMagick;

namespace ViajantesTurismo.Catalog.UnitTests;

internal static class CatalogTestImages
{
    private const ulong MaxDecodedPixelCount = 40_000_000;
    private const ulong MaxDecodedSize = 8_000;

    public static byte[] CreateJpeg(uint width, uint height)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                ResourceLimits.Width = MaxDecodedSize;
                ResourceLimits.Height = MaxDecodedSize;
                ResourceLimits.Area = MaxDecodedPixelCount;

                using var image = new MagickImage(MagickColors.Blue, width, height);
                image.Format = MagickFormat.Jpeg;

                return image.ToByteArray();
            }
            catch (MagickImageErrorException ex)
                when (attempt < 2 && ex.Message.Contains("WidthOrHeightExceedsLimit", StringComparison.Ordinal))
            {
                Thread.Sleep(20);
            }
        }

        using var finalImage = new MagickImage(MagickColors.Blue, width, height);
        finalImage.Format = MagickFormat.Jpeg;

        return finalImage.ToByteArray();
    }
}
