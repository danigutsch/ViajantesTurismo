namespace ViajantesTurismo.Catalog.Contracts;

/// <summary>
/// Localized public content variant returned by the Catalog API.
/// </summary>
public sealed class PublicContentVariantDto
{
    /// <summary>
    /// Gets or sets the content language.
    /// </summary>
    public PublicContentLanguageDto Language { get; set; }

    /// <summary>
    /// Gets or sets the public title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the public body text.
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional SEO title.
    /// </summary>
    public string? SeoTitle { get; set; }

    /// <summary>
    /// Gets or sets the optional meta description.
    /// </summary>
    public string? MetaDescription { get; set; }

    /// <summary>
    /// Gets or sets the optional sharing summary.
    /// </summary>
    public string? ShareSummary { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this variant requires human review.
    /// </summary>
    public bool RequiresHumanReview { get; set; }
}
