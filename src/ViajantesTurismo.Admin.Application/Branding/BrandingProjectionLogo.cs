namespace ViajantesTurismo.Admin.Application.Branding;

/// <summary>The re-encoded logo the Admin projection stores locally so documents never depend on a live fetch.</summary>
/// <param name="Sha256">The lowercase hexadecimal SHA-256 of <paramref name="Content" />.</param>
/// <param name="ContentType">The media type of <paramref name="Content" />.</param>
/// <param name="Content">The logo bytes.</param>
public sealed record BrandingProjectionLogo(string Sha256, string ContentType, ReadOnlyMemory<byte> Content)
{
    /// <summary>Compares logos by content address, media type and the bytes themselves, not by buffer identity.</summary>
    /// <param name="other">The logo to compare with.</param>
    /// <returns><see langword="true" /> when both logos have the same hash, media type and bytes.</returns>
    public bool Equals(BrandingProjectionLogo? other) =>
        other is not null
        && string.Equals(Sha256, other.Sha256, StringComparison.Ordinal)
        && string.Equals(ContentType, other.ContentType, StringComparison.Ordinal)
        && Content.Span.SequenceEqual(other.Content.Span);

    /// <summary>Hashes the content address and media type; the hash already commits to the bytes.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode() => HashCode.Combine(Sha256, ContentType);

    /// <summary>Prints the content address, media type and size, never the roughly 40 KiB of bytes.</summary>
    /// <returns>The description.</returns>
    public override string ToString() =>
        $"BrandingProjectionLogo {{ Sha256 = {Sha256}, ContentType = {ContentType}, ContentLength = {Content.Length} }}";
}
