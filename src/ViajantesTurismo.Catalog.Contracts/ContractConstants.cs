namespace ViajantesTurismo.Catalog.Contracts;

/// <summary>
/// Contains constant values used in the contract definitions.
/// </summary>
public static class ContractConstants
{
    /// <summary>
    /// The maximum length for default string fields such as identifiers.
    /// </summary>
    public const int MaxDefaultLength = 64;

    /// <summary>
    /// The maximum length for names and titles.
    /// </summary>
    public const int MaxNameLength = 128;

    /// <summary>
    /// The maximum length for public URL slugs.
    /// </summary>
    public const int MaxSlugLength = 128;

    /// <summary>
    /// The maximum length for image alternative text.
    /// </summary>
    public const int MaxAltTextLength = 256;

    /// <summary>
    /// The maximum length for image captions.
    /// </summary>
    public const int MaxCaptionLength = 256;

    /// <summary>
    /// The maximum length for media checksums.
    /// </summary>
    public const int MaxChecksumLength = 128;

    /// <summary>
    /// The maximum length for media content types.
    /// </summary>
    public const int MaxContentTypeLength = 128;

    /// <summary>
    /// The maximum length for media attribution text.
    /// </summary>
    public const int MaxAttributionLength = 256;

    /// <summary>
    /// The maximum length for media copyright notices.
    /// </summary>
    public const int MaxCopyrightLength = 256;

    /// <summary>
    /// The maximum length for public content body text.
    /// </summary>
    public const int MaxBodyLength = 4000;
}
