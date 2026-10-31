using System.Security.Cryptography;
using ViajantesTurismo.Admin.Application.Branding;

namespace ViajantesTurismo.Admin.Testing.Builders;

/// <summary>Provides a tiny WebP-signed logo, its hash and its data URI for document tests.</summary>
public static class DocumentLogoTestData
{
    /// <summary>The media type of the test logo.</summary>
    public const string ContentType = "image/webp";

    /// <summary>A RIFF/WEBP-signed payload; the sanitizer proves the signature, the intake proves decodability.</summary>
    public static readonly byte[] Content =
    [
        0x52, 0x49, 0x46, 0x46, 0x0C, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50, 0x56, 0x50, 0x38, 0x20, 0x01, 0x02, 0x03, 0x04
    ];

    /// <summary>A second, different logo used to replace the first one.</summary>
    public static readonly byte[] OtherContent =
    [
        0x52, 0x49, 0x46, 0x46, 0x0C, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50, 0x56, 0x50, 0x38, 0x20, 0x09, 0x08, 0x07, 0x06
    ];

    /// <summary>The lowercase hexadecimal SHA-256 of <see cref="Content" />.</summary>
    public static string Sha256 => Convert.ToHexStringLower(SHA256.HashData(Content));

    /// <summary>The test logo as a base64 data URI.</summary>
    public static Uri DataUri => new($"data:{ContentType};base64,{Convert.ToBase64String(Content)}", UriKind.Absolute);

    /// <summary>The test logo as a projection logo.</summary>
    public static BrandingProjectionLogo Logo => new(Sha256, ContentType, Content);

    /// <summary>Builds a projection logo for arbitrary bytes with a matching hash.</summary>
    public static BrandingProjectionLogo LogoFor(byte[] content) =>
        new(Convert.ToHexStringLower(SHA256.HashData(content)), ContentType, content);
}
