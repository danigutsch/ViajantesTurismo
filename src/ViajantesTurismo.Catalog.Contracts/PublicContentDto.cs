namespace ViajantesTurismo.Catalog.Contracts;

/// <summary>
/// Editable localized public website content.
/// </summary>
public sealed class PublicContentDto
{
    /// <summary>
    /// Gets or sets the stable content key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source language entered by the editor.
    /// </summary>
    public PublicContentLanguageDto SourceLanguage { get; set; }

    /// <summary>
    /// Gets or sets the localized content variants.
    /// </summary>
    public ICollection<PublicContentVariantDto> Variants { get; init; } = [];

    /// <summary>
    /// Gets or sets the publication state.
    /// </summary>
    public string PublicationState { get; set; } = string.Empty;
}
