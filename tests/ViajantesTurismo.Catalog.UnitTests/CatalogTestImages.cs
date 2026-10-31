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

    /// <summary>Creates a solid blue still PNG of the given pixel size.</summary>
    public static byte[] CreatePng(uint width, uint height)
    {
        using var image = new MagickImage(MagickColors.Blue, width, height);
        image.Format = MagickFormat.Png;

        return image.ToByteArray();
    }

    /// <summary>Creates a two-frame animated WebP of the given pixel size.</summary>
    public static byte[] CreateAnimatedWebP(uint width, uint height) => CreateAnimation(width, height, MagickFormat.WebP);

    private static byte[] CreateAnimation(uint width, uint height, MagickFormat format)
    {
        using var frames = new MagickImageCollection();
        foreach (var color in new[] { MagickColors.Blue, MagickColors.Red })
        {
            var frame = new MagickImage(color, width, height)
            {
                AnimationDelay = 10
            };
            frames.Add(frame);
        }

        using var output = new MemoryStream();
        frames.Write(output, format);

        return output.ToArray();
    }
}
