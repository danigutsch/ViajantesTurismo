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

    /// <summary>Creates a fully transparent PNG that is forced to store an alpha channel.</summary>
    public static byte[] CreateTransparentPng(uint width, uint height)
    {
        using var image = new MagickImage(MagickColors.Transparent, width, height);
        image.Settings.SetDefine("png:color-type", "6");
        image.Format = MagickFormat.Png;
        return image.ToByteArray();
    }

    /// <summary>Creates a PNG whose alpha fades radially from opaque in the centre to transparent at the corners.</summary>
    public static byte[] CreateSoftEdgedLogoPng(uint width, uint height)
    {
        using var image = new MagickImage(MagickColors.SeaGreen, width, height);
        using var mask = new MagickImage("radial-gradient:white-black", width, height);
        image.Composite(mask, CompositeOperator.CopyAlpha);
        image.Settings.SetDefine("png:color-type", "6");
        image.Format = MagickFormat.Png;
        return image.ToByteArray();
    }

    /// <summary>Creates a PNG logo with a colour gradient under a radial alpha fade, a worst case for the size budget.</summary>
    public static byte[] CreateGradientLogoPng(uint width, uint height)
    {
        using var image = new MagickImage("gradient:red-blue", width, height);
        using var mask = new MagickImage("radial-gradient:white-black", width, height);
        image.Composite(mask, CompositeOperator.CopyAlpha);
        image.Settings.SetDefine("png:color-type", "6");
        image.Format = MagickFormat.Png;
        return image.ToByteArray();
    }

    /// <summary>Creates an animated WebP with two differently coloured frames.</summary>
    public static byte[] CreateAnimatedWebP(uint width, uint height)
    {
        using var frames = new MagickImageCollection();
        frames.Add(new MagickImage(MagickColors.SeaGreen, width, height) { AnimationDelay = 10 });
        frames.Add(new MagickImage(MagickColors.Crimson, width, height) { AnimationDelay = 10 });
        return frames.ToByteArray(MagickFormat.WebP);
    }

    /// <summary>Returns true when the encoded image carries an alpha channel.</summary>
    public static bool HasAlpha(byte[] image)
    {
        using var decoded = new MagickImage(image);
        return decoded.HasAlpha;
    }

    /// <summary>Returns the number of frames in the encoded image.</summary>
    public static int FrameCount(byte[] image)
    {
        using var frames = new MagickImageCollection(image);
        return frames.Count;
    }

    /// <summary>Returns the pixel width of the encoded image.</summary>
    public static uint WidthOf(byte[] image)
    {
        using var decoded = new MagickImage(image);
        return decoded.Width;
    }

    /// <summary>Returns the alpha of one pixel on a 0 (transparent) to 255 (opaque) scale.</summary>
    public static double AlphaAt(byte[] image, uint x, uint y)
    {
        using var decoded = new MagickImage(image);
        using var pixels = decoded.GetPixels();
        var color = pixels.GetPixel((int)x, (int)y).ToColor();
        return color is null ? 255 : color.A * 255.0 / Quantum.Max;
    }

    /// <summary>Returns the largest absolute alpha difference over a grid of sample points, on a 0 to 255 scale.</summary>
    public static double MaxAlphaDifference(byte[] first, byte[] second)
    {
        using var left = new MagickImage(first);
        using var right = new MagickImage(second);
        using var leftPixels = left.GetPixels();
        using var rightPixels = right.GetPixels();
        var worst = 0.0;
        for (var y = 0; y < (int)left.Height; y += 2)
        {
            for (var x = 0; x < (int)left.Width; x += 2)
            {
                var a = leftPixels.GetPixel(x, y).ToColor()?.A * 255.0 / Quantum.Max ?? 255;
                var b = rightPixels.GetPixel(x, y).ToColor()?.A * 255.0 / Quantum.Max ?? 255;
                worst = Math.Max(worst, Math.Abs(a - b));
            }
        }

        return worst;
    }
}
