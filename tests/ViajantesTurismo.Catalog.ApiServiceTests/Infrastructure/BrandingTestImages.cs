using System.Buffers.Binary;
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

    /// <summary>Creates an APNG-shaped PNG: a valid still PNG with an <c>acTL</c> animation-control chunk before the image data.</summary>
    public static byte[] CreateAnimatedPng(uint width, uint height)
    {
        var still = Create(MagickFormat.Png, width, height);
        const int signatureLength = 8;
        var ihdrChunkLength = 12 + (int)BinaryPrimitives.ReadUInt32BigEndian(still.AsSpan(signatureLength, 4));
        var insertAt = signatureLength + ihdrChunkLength;
        var chunk = new byte[20];
        BinaryPrimitives.WriteUInt32BigEndian(chunk.AsSpan(0, 4), 8);
        "acTL"u8.CopyTo(chunk.AsSpan(4, 4));
        BinaryPrimitives.WriteUInt32BigEndian(chunk.AsSpan(8, 4), 2);
        BinaryPrimitives.WriteUInt32BigEndian(chunk.AsSpan(12, 4), 0);
        BinaryPrimitives.WriteUInt32BigEndian(chunk.AsSpan(16, 4), Crc32(chunk.AsSpan(4, 12)));
        return [.. still.AsSpan(0, insertAt), .. chunk, .. still.AsSpan(insertAt)];
    }

    /// <summary>Creates the <see cref="CreateAnimatedPng"/> bytes with the <c>acTL</c> chunk removed again, a still PNG.</summary>
    public static byte[] CreateStillPngWithoutAnimationControl(uint width, uint height)
    {
        var animated = CreateAnimatedPng(width, height);
        const int signatureLength = 8;
        var ihdrEnd = signatureLength + 12 + (int)BinaryPrimitives.ReadUInt32BigEndian(animated.AsSpan(signatureLength, 4));
        const int animationControlLength = 20;
        return [.. animated.AsSpan(0, ihdrEnd), .. animated.AsSpan(ihdrEnd + animationControlLength)];
    }

    /// <summary>Creates a still PNG with an <c>acTL</c> chunk after the image data, where the specification ignores it.</summary>
    public static byte[] CreatePngWithAnimationControlAfterImageData(uint width, uint height)
    {
        var still = Create(MagickFormat.Png, width, height);
        const int iendChunkLength = 12;
        var insertAt = still.Length - iendChunkLength;
        var chunk = new byte[20];
        BinaryPrimitives.WriteUInt32BigEndian(chunk.AsSpan(0, 4), 8);
        "acTL"u8.CopyTo(chunk.AsSpan(4, 4));
        BinaryPrimitives.WriteUInt32BigEndian(chunk.AsSpan(8, 4), 2);
        BinaryPrimitives.WriteUInt32BigEndian(chunk.AsSpan(16, 4), Crc32(chunk.AsSpan(4, 12)));
        return [.. still.AsSpan(0, insertAt), .. chunk, .. still.AsSpan(insertAt)];
    }

    /// <summary>Creates a PNG whose first chunk after the header declares a length of <c>0xFFFFFFF0</c>.</summary>
    public static byte[] CreatePngWithOversizedChunk(uint width, uint height)
    {
        var still = Create(MagickFormat.Png, width, height);
        const int signatureLength = 8;
        var insertAt = signatureLength + 12 + (int)BinaryPrimitives.ReadUInt32BigEndian(still.AsSpan(signatureLength, 4));
        var header = new byte[8];
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(0, 4), 0xFFFFFFF0);
        "tEXt"u8.CopyTo(header.AsSpan(4, 4));
        return [.. still.AsSpan(0, insertAt), .. header, .. still.AsSpan(insertAt)];
    }

    /// <summary>Creates a PNG that ends after five bytes of a chunk header.</summary>
    public static byte[] CreatePngWithTruncatedChunkHeader(uint width, uint height)
    {
        var still = Create(MagickFormat.Png, width, height);
        const int signatureLength = 8;
        var ihdrEnd = signatureLength + 12 + (int)BinaryPrimitives.ReadUInt32BigEndian(still.AsSpan(signatureLength, 4));
        return still[..(ihdrEnd + 5)];
    }

    /// <summary>Creates a WebP whose RIFF size and first chunk size are both near <see cref="uint.MaxValue"/>.</summary>
    public static byte[] CreateWebPWithOversizedSizes() =>
        WebPFile(0xFFFFFFF0, WebPChunk("VP8 "u8, new byte[16], 0xFFFFFFF0));

    /// <summary>Creates a WebP that ends after five bytes of a chunk header.</summary>
    public static byte[] CreateWebPWithTruncatedChunkHeader() =>
        WebPFile(9, [.. "VP8 "u8, 0x01]);

    /// <summary>Creates a WebP whose <c>VP8X</c> chunk has the animation flag set.</summary>
    public static byte[] CreateWebPWithAnimationFlag() =>
        WebPFile(null, WebPChunk("VP8X"u8, [0x02, 0, 0, 0, 31, 0, 0, 31, 0, 0], null));

    /// <summary>Creates a WebP whose <c>VP8X</c> animation flag is cleared but that carries a top-level <c>ANMF</c> chunk.</summary>
    public static byte[] CreateWebPWithAnimationFrameButNoFlag() =>
        WebPFile(null, [.. WebPChunk("VP8X"u8, [0, 0, 0, 0, 31, 0, 0, 31, 0, 0], null), .. WebPChunk("ANMF"u8, new byte[16], null)]);

    /// <summary>Creates a real still WebP followed by bytes outside the RIFF payload that look like an <c>ANMF</c> chunk.</summary>
    public static byte[] CreateStillWebPWithTrailingAnimationFrame(uint width, uint height) =>
        [.. Create(MagickFormat.WebP, width, height), .. WebPChunk("ANMF"u8, new byte[16], null)];

    private static byte[] WebPChunk(ReadOnlySpan<byte> type, byte[] payload, uint? declaredSize)
    {
        var chunk = new byte[8 + payload.Length];
        type.CopyTo(chunk);
        BinaryPrimitives.WriteUInt32LittleEndian(chunk.AsSpan(4, 4), declaredSize ?? (uint)payload.Length);
        payload.CopyTo(chunk.AsSpan(8));
        return chunk;
    }

    private static byte[] WebPFile(uint? riffSize, byte[] body)
    {
        var file = new byte[12 + body.Length];
        "RIFF"u8.CopyTo(file);
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(4, 4), riffSize ?? (uint)(4 + body.Length));
        "WEBP"u8.CopyTo(file.AsSpan(8));
        body.CopyTo(file.AsSpan(12));
        return file;
    }

    private static uint Crc32(ReadOnlySpan<byte> data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var value in data)
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
            }
        }

        return ~crc;
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
