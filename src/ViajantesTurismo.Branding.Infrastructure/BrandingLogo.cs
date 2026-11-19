using System.Security.Cryptography;

namespace ViajantesTurismo.Branding.Infrastructure;

/// <summary>A normalized Branding logo: re-encoded raster bytes addressed by the SHA-256 of those bytes.</summary>
public sealed record BrandingLogo
{
    /// <summary>Media type of every re-encoded logo.</summary>
    public const string EncodedContentType = "image/webp";

    private BrandingLogo(string sha256, ReadOnlyMemory<byte> content)
    {
        Sha256 = sha256;
        Content = content;
    }

    /// <summary>Gets the lowercase hexadecimal SHA-256 of <see cref="Content" />.</summary>
    public string Sha256
    {
        get;
    }

    /// <summary>Gets the media type of the re-encoded bytes.</summary>
    public string ContentType
    {
        get;
    } = EncodedContentType;

    /// <summary>Gets the re-encoded image bytes.</summary>
    public ReadOnlyMemory<byte> Content
    {
        get;
    }

    /// <summary>Creates a logo from re-encoded bytes, computing its content address.</summary>
    /// <param name="content">The re-encoded image bytes.</param>
    /// <returns>The content-addressed logo.</returns>
    public static BrandingLogo FromEncodedBytes(ReadOnlyMemory<byte> content) =>
        new(ComputeSha256(content), content);

    /// <summary>Restores a WebP logo only when its stored bytes match its recorded content address.</summary>
    internal static BrandingLogo? FromStoredBytes(string sha256, string contentType, ReadOnlyMemory<byte> content)
    {
        if (!string.Equals(contentType, EncodedContentType, StringComparison.Ordinal))
        {
            return null;
        }

        var actualHash = ComputeSha256(content);
        return string.Equals(sha256, actualHash, StringComparison.Ordinal) ? new BrandingLogo(actualHash, content) : null;
    }

    private static string ComputeSha256(ReadOnlyMemory<byte> content) =>
        Convert.ToHexStringLower(SHA256.HashData(content.Span));
}
