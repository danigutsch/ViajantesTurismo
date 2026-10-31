namespace ViajantesTurismo.Branding.Infrastructure;

/// <summary>A normalized Branding logo: re-encoded raster bytes addressed by the SHA-256 of those bytes.</summary>
/// <param name="Sha256">The lowercase hexadecimal SHA-256 of <paramref name="Content" />.</param>
/// <param name="ContentType">The media type of the re-encoded bytes.</param>
/// <param name="Content">The re-encoded image bytes.</param>
public sealed record BrandingLogo(string Sha256, string ContentType, ReadOnlyMemory<byte> Content)
{
    /// <summary>Media type of every re-encoded logo.</summary>
    public const string EncodedContentType = "image/webp";

    /// <summary>Creates a logo from re-encoded bytes, computing its content address.</summary>
    /// <param name="content">The re-encoded image bytes.</param>
    /// <returns>The content-addressed logo.</returns>
    public static BrandingLogo FromEncodedBytes(ReadOnlyMemory<byte> content) =>
        new(Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(content.Span)), EncodedContentType, content);
}
