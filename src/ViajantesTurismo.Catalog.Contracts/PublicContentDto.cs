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
    /// Gets or sets the English content variant.
    /// </summary>
    public PublicContentVariantDto EnUs { get; set; } = new();

    /// <summary>
    /// Gets or sets the Brazilian Portuguese content variant.
    /// </summary>
    public PublicContentVariantDto PtBr { get; set; } = new();

    /// <summary>
    /// Gets or sets the publication state.
    /// </summary>
    public string PublicationState { get; set; } = string.Empty;
}
