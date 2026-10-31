namespace ViajantesTurismo.Branding.Infrastructure;

/// <summary>An immutable stored logo, keyed by the SHA-256 of its re-encoded bytes and never updated.</summary>
internal sealed class BrandingLogoRecord
{
    private BrandingLogoRecord(string sha256, string contentType, byte[] content, DateTimeOffset storedAt)
    {
        Sha256 = sha256;
        ContentType = contentType;
        Content = content;
        StoredAt = storedAt;
    }

    /// <summary>Lowercase hexadecimal SHA-256 of <see cref="Content" />; the primary key.</summary>
    public string Sha256
    {
        get; private init;
    }

    /// <summary>Media type of the stored bytes.</summary>
    public string ContentType
    {
        get; private init;
    }

    /// <summary>Re-encoded image bytes.</summary>
    public byte[] Content
    {
        get; private init;
    }

    /// <summary>When the bytes were first stored.</summary>
    public DateTimeOffset StoredAt
    {
        get; private init;
    }

    /// <summary>Creates the record for a logo stored at the given time.</summary>
    public static BrandingLogoRecord Create(BrandingLogo logo, DateTimeOffset storedAt)
    {
        ArgumentNullException.ThrowIfNull(logo);

        return new BrandingLogoRecord(logo.Sha256, logo.ContentType, logo.Content.ToArray(), storedAt);
    }

    /// <summary>Converts the record to the logo value.</summary>
    public BrandingLogo ToLogo() => new(Sha256, ContentType, Content);
}
