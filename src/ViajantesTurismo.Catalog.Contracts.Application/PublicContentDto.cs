using System.Text.Json.Serialization;
using SharedKernel.BuildingBlocks;

namespace ViajantesTurismo.Catalog.Contracts.Application;

/// <summary>
/// Editable localized public website content.
/// </summary>
public sealed class PublicContentDto
{
    /// <summary>Gets links for this managed content entry.</summary>
    [JsonPropertyName("links")]
    public IReadOnlyList<HypermediaLink> Links { get; init; } = [];

    /// <summary>
    /// Gets or sets the stable content key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the language tag of the source culture entered by the editor.
    /// </summary>
    public string SourceLanguage
    {
        get; set;
    } = string.Empty;

    /// <summary>
    /// Gets or sets the localized content variants.
    /// </summary>
    public ICollection<PublicContentVariantDto> Variants { get; init; } = [];

    /// <summary>
    /// Gets or sets the publication state.
    /// </summary>
    public string PublicationState { get; set; } = string.Empty;
}
