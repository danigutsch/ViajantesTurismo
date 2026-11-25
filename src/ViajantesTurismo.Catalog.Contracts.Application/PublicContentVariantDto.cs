using System.Text.Json.Serialization;
using SharedKernel.BuildingBlocks;

namespace ViajantesTurismo.Catalog.Contracts.Application;

/// <summary>
/// Localized public content variant returned by the Catalog API.
/// </summary>
public sealed class PublicContentVariantDto
{
    /// <summary>Gets links when this localized variant is returned as a resource.</summary>
    [JsonPropertyName("links")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<HypermediaLink>? Links
    {
        get; init;
    }

    /// <summary>
    /// Gets or sets the content language tag, for example <c>pt-BR</c>.
    /// </summary>
    public string Language
    {
        get; set;
    } = string.Empty;

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
    public string? SeoTitle
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the optional meta description.
    /// </summary>
    public string? MetaDescription
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the optional sharing summary.
    /// </summary>
    public string? ShareSummary
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether this variant requires human review.
    /// </summary>
    public bool RequiresHumanReview
    {
        get; set;
    }
}
