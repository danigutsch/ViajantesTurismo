namespace ViajantesTurismo.Catalog.Contracts;

/// <summary>
/// Request to create or update editable public website content.
/// </summary>
public sealed class UpsertPublicContentRequest
{
    /// <summary>
    /// Gets or sets the source language entered by the editor.
    /// </summary>
    public PublicContentLanguageDto SourceLanguage { get; set; }

    /// <summary>
    /// Gets or sets the localized content variants.
    /// </summary>
    public ICollection<PublicContentVariantDto> Variants { get; init; } = [];
}
