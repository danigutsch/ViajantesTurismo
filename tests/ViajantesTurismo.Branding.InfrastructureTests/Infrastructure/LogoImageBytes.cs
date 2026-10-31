using System.Buffers.Binary;
using ImageMagick;

namespace ViajantesTurismo.Branding.InfrastructureTests.Infrastructure;

/// <summary>Builds image bytes for Branding logo intake tests: real decodable images and hand-built chunk sequences.</summary>
internal static class LogoImageBytes
{
    /// <summary>Length of the PNG signature.</summary>
    public const int PngSignatureLength = 8;

    /// <summary>Length of the IHDR chunk written by <see cref="HandBuiltPng"/>: header, 13 data bytes, CRC.</summary>
    public const int PngIhdrChunkLength = 25;

    /// <summary>Length of the RIFF header (RIFF, size, WEBP).</summary>
    public const int WebPRiffHeaderLength = 12;

    /// <summary>Creates a decodable solid-color image.</summary>
    public static byte[] Real(MagickFormat format, uint edge = 16)
    {
        using var image = new MagickImage(MagickColors.SeaGreen, edge, edge);
        image.Format = format;
        return image.ToByteArray();
    }

    /// <summary>Creates a decodable APNG-shaped PNG: a still PNG with an <c>acTL</c> chunk before the image data.</summary>
    public static byte[] RealAnimatedPng()
    {
        var still = Real(MagickFormat.Png);
        var insertAt = PngSignatureLength + 12 + (int)BinaryPrimitives.ReadUInt32BigEndian(still.AsSpan(PngSignatureLength, 4));
        return [.. still.AsSpan(0, insertAt), .. PngChunk("acTL"u8, new byte[8]), .. still.AsSpan(insertAt)];
    }

    /// <summary>Creates a PNG chunk sequence the chunk walk understands: signature, IHDR, optionally <c>acTL</c>, IDAT, IEND. CRCs are zero.</summary>
    public static byte[] HandBuiltPng(bool animated) =>
    [
        .. new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A },
        .. PngChunk("IHDR"u8, new byte[13]),
        .. animated ? PngChunk("acTL"u8, new byte[8]) : [],
        .. PngChunk("IDAT"u8, new byte[10]),
        .. PngChunk("IEND"u8, [])
    ];

    /// <summary>Creates a WebP chunk sequence the chunk walk understands: RIFF header, <c>VP8X</c> with the animation flag, <c>VP8 </c>.</summary>
    public static byte[] HandBuiltWebP(bool animationFlag)
    {
        byte[] body =
        [
            .. WebPChunk("VP8X"u8, [(byte)(animationFlag ? 0x02 : 0), 0, 0, 0, 0, 0, 0, 0, 0, 0]),
            .. WebPChunk("VP8 "u8, new byte[16])
        ];
        var file = new byte[WebPRiffHeaderLength + body.Length];
        "RIFF"u8.CopyTo(file);
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(4, 4), (uint)(4 + body.Length));
        "WEBP"u8.CopyTo(file.AsSpan(8));
        body.CopyTo(file.AsSpan(WebPRiffHeaderLength));
        return file;
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

    /// <summary>Creates a WebP chunk with an even-padded payload.</summary>
    public static byte[] WebPChunk(ReadOnlySpan<byte> type, byte[] payload)
    {
        var chunk = new byte[8 + payload.Length + (payload.Length & 1)];
        type.CopyTo(chunk);
        BinaryPrimitives.WriteUInt32LittleEndian(chunk.AsSpan(4, 4), (uint)payload.Length);
        payload.CopyTo(chunk.AsSpan(8));
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
