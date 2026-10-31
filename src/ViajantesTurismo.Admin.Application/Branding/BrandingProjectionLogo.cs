using System.Diagnostics.CodeAnalysis;

namespace ViajantesTurismo.Admin.Application.Branding;

/// <summary>The re-encoded logo the Admin projection stores locally so documents never depend on a live fetch.</summary>
/// <param name="Sha256">The lowercase hexadecimal SHA-256 of <paramref name="Content" />.</param>
/// <param name="ContentType">The media type of <paramref name="Content" />.</param>
/// <param name="Content">The logo bytes.</param>
[SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "The logo is an immutable byte payload mapped to one bytea column.")]
public sealed record BrandingProjectionLogo(string Sha256, string ContentType, byte[] Content)
{
    /// <summary>Compares logos by content address and media type; the hash already commits to the bytes.</summary>
    /// <param name="other">The logo to compare with.</param>
    /// <returns><see langword="true" /> when both logos have the same hash and media type.</returns>
    public bool Equals(BrandingProjectionLogo? other) =>
        other is not null
        && string.Equals(Sha256, other.Sha256, StringComparison.Ordinal)
        && string.Equals(ContentType, other.ContentType, StringComparison.Ordinal);

    /// <summary>Hashes the content address and media type.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode() => HashCode.Combine(Sha256, ContentType);
}
