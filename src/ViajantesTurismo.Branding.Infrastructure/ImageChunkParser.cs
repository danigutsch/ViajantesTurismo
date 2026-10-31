using System.Buffers.Binary;

namespace ViajantesTurismo.Branding.Infrastructure;

/// <summary>
/// Pure functions over chunk headers of PNG and WebP (RIFF) files. They never touch a stream: every length and offset is
/// handled in <see cref="long"/>, and every overrun or truncation is reported as <see langword="false"/> so the caller
/// can fail closed before it assigns a stream position.
/// </summary>
internal static class ImageChunkParser
{
    /// <summary>Length of a PNG chunk header: a 4-byte big-endian length and a 4-byte type.</summary>
    public const int PngChunkHeaderLength = 8;

    /// <summary>Length of the CRC that follows every PNG chunk's data.</summary>
    public const int PngChunkCrcLength = 4;

    /// <summary>Length of a WebP chunk header: a 4-byte type and a 4-byte little-endian size.</summary>
    public const int WebPChunkHeaderLength = 8;

    /// <summary>Length of the RIFF header: <c>RIFF</c>, the 4-byte size and <c>WEBP</c>.</summary>
    public const int WebPRiffHeaderLength = 12;

    private const long PngMaxChunkLength = int.MaxValue;

    /// <summary>Parses a PNG chunk header into its role and declared data length.</summary>
    /// <param name="header">The header bytes; at least <see cref="PngChunkHeaderLength"/> are needed.</param>
    /// <param name="kind">The chunk role.</param>
    /// <param name="length">The declared data length, excluding header and CRC.</param>
    /// <returns><see langword="false"/> when the header is truncated or declares a length above 2^31-1, the PNG maximum.</returns>
    public static bool TryParsePngHeader(ReadOnlySpan<byte> header, out ImageChunkKind kind, out uint length)
    {
        kind = ImageChunkKind.Other;
        length = 0;
        if (header.Length < PngChunkHeaderLength)
        {
            return false;
        }

        length = BinaryPrimitives.ReadUInt32BigEndian(header);
        if (length > PngMaxChunkLength)
        {
            return false;
        }

        var type = header.Slice(4, 4);
        kind = type.SequenceEqual("acTL"u8)
            ? ImageChunkKind.Animation
            : type.SequenceEqual("IDAT"u8) || type.SequenceEqual("IEND"u8) ? ImageChunkKind.EndOfControlRegion : ImageChunkKind.Other;
        return true;
    }

    /// <summary>Computes where the next PNG chunk starts.</summary>
    /// <param name="position">The start of the current chunk.</param>
    /// <param name="length">The declared data length of the current chunk.</param>
    /// <param name="streamLength">The total length of the stream.</param>
    /// <param name="next">The start of the next chunk, or <paramref name="streamLength"/> when the chunk overruns.</param>
    /// <returns><see langword="false"/> when the chunk, with its CRC, runs past the end of the stream.</returns>
    public static bool TryGetNextPngPosition(long position, uint length, long streamLength, out long next)
    {
        var total = PngChunkHeaderLength + (long)length + PngChunkCrcLength;
        var fits = position >= 0 && streamLength >= position && streamLength - position >= total;
        next = fits ? position + total : streamLength;
        return fits;
    }

    /// <summary>Parses a WebP chunk header into its role and declared payload size.</summary>
    /// <param name="header">The header bytes; at least <see cref="WebPChunkHeaderLength"/> are needed.</param>
    /// <param name="kind">The chunk role.</param>
    /// <param name="size">The declared payload size, excluding header and padding.</param>
    /// <returns><see langword="false"/> when the header is truncated.</returns>
    public static bool TryParseWebPHeader(ReadOnlySpan<byte> header, out ImageChunkKind kind, out uint size)
    {
        kind = ImageChunkKind.Other;
        size = 0;
        if (header.Length < WebPChunkHeaderLength)
        {
            return false;
        }

        size = BinaryPrimitives.ReadUInt32LittleEndian(header[4..]);
        var type = header[..4];
        kind = type.SequenceEqual("ANIM"u8) || type.SequenceEqual("ANMF"u8)
            ? ImageChunkKind.Animation
            : type.SequenceEqual("VP8X"u8) ? ImageChunkKind.ExtendedHeader : ImageChunkKind.Other;
        return true;
    }

    /// <summary>Computes the end of the RIFF payload, bounded by the stream length.</summary>
    /// <param name="riffSizeField">The 4 bytes of the RIFF size field, or fewer when the file is truncated.</param>
    /// <param name="streamLength">The total length of the stream.</param>
    /// <param name="end">The offset where the chunk region ends.</param>
    /// <returns><see langword="false"/> when the size field is truncated.</returns>
    public static bool TryGetWebPEnd(ReadOnlySpan<byte> riffSizeField, long streamLength, out long end)
    {
        end = 0;
        if (riffSizeField.Length < sizeof(uint))
        {
            return false;
        }

        end = Math.Min(streamLength, WebPChunkHeaderLength + (long)BinaryPrimitives.ReadUInt32LittleEndian(riffSizeField));
        return true;
    }

    /// <summary>Computes where the next WebP chunk starts, including the padding byte of an odd payload.</summary>
    /// <param name="position">The start of the current chunk.</param>
    /// <param name="size">The declared payload size of the current chunk.</param>
    /// <param name="end">The end of the chunk region from <see cref="TryGetWebPEnd"/>.</param>
    /// <param name="next">The start of the next chunk, or <paramref name="end"/> when the payload overruns.</param>
    /// <returns><see langword="false"/> when the payload runs past <paramref name="end"/>.</returns>
    public static bool TryGetNextWebPPosition(long position, uint size, long end, out long next)
    {
        var fits = position >= 0 && end >= position && end - position >= WebPChunkHeaderLength + (long)size;
        next = fits ? position + WebPChunkHeaderLength + size + (size & 1) : end;
        return fits;
    }
}
