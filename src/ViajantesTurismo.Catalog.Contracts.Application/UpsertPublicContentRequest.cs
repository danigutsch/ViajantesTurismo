namespace ViajantesTurismo.Catalog.Contracts.Application;

/// <summary>
/// Request to create or update editable public website content.
/// </summary>
public sealed class UpsertPublicContentRequest
{
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
}
