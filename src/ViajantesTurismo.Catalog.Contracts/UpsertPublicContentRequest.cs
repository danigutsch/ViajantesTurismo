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
    /// Gets or sets the English content variant.
    /// </summary>
    public PublicContentVariantDto EnUs { get; set; } = new();

    /// <summary>
    /// Gets or sets the Brazilian Portuguese content variant.
    /// </summary>
    public PublicContentVariantDto PtBr { get; set; } = new();
}
