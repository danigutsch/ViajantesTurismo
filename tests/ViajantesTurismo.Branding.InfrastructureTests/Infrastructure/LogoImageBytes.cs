using System.Buffers.Binary;
using ImageMagick;

namespace ViajantesTurismo.Branding.InfrastructureTests.Infrastructure;

/// <summary>Builds image bytes for Branding logo intake tests: real decodable images and hand-built byte sequences.</summary>
internal static class LogoImageBytes
{
    /// <summary>Length of the PNG signature.</summary>
    public const int PngSignatureLength = 8;

    /// <summary>Creates a decodable solid-color image.</summary>
    public static byte[] Real(MagickFormat format, uint edge = 16)
    {
        using var image = new MagickImage(MagickColors.SeaGreen, edge, edge);
        image.Format = format;
        return image.ToByteArray();
    }

    /// <summary>Creates a reproducible 1024 by 512 photographic-noise fixture within the raw upload budget.</summary>
    public static byte[] NoisyJpeg()
    {
        var header = "P6\n1024 512\n255\n"u8;
        var imageData = new byte[header.Length + (1024 * 512 * 3)];
        header.CopyTo(imageData);
        new Random(42).NextBytes(imageData.AsSpan(header.Length));
        using var image = new MagickImage(imageData)
        {
            Quality = 84
        };
        return image.ToByteArray(MagickFormat.Jpeg);
    }

    /// <summary>Creates a decodable APNG-shaped PNG: a still PNG with an <c>acTL</c> chunk before the image data.</summary>
    public static byte[] RealAnimatedPng()
    {
        var still = Real(MagickFormat.Png);
        var insertAt = PngSignatureLength + 12 + (int)BinaryPrimitives.ReadUInt32BigEndian(still.AsSpan(PngSignatureLength, 4));
        return [.. still.AsSpan(0, insertAt), .. PngChunk("acTL"u8, new byte[8]), .. still.AsSpan(insertAt)];
    }

    /// <summary>Creates a PNG chunk with a zero CRC.</summary>
    public static byte[] PngChunk(ReadOnlySpan<byte> type, byte[] data)
    {
        var chunk = new byte[8 + data.Length + 4];
        BinaryPrimitives.WriteUInt32BigEndian(chunk.AsSpan(0, 4), (uint)data.Length);
        type.CopyTo(chunk.AsSpan(4));
        data.CopyTo(chunk.AsSpan(8));
        return chunk;
    }

    /// <summary>Creates a PNG-signed body of the given length that is not a decodable image.</summary>
    public static byte[] PngSignedFiller(int length)
    {
        var bytes = new byte[length];
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }.AsSpan(0, Math.Min(8, length)).CopyTo(bytes);
        return bytes;
    }
}
