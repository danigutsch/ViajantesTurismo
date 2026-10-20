namespace ViajantesTurismo.Catalog.Contracts.Http;

/// <summary>
/// Describes a catalog tour image uploaded through the management API.
/// </summary>
/// <param name="Content">The image content.</param>
/// <param name="FileName">The client-supplied filename used only for validation.</param>
/// <param name="ContentType">The client-supplied media type used only for validation.</param>
/// <param name="AltText">The editor-provided accessible description.</param>
/// <param name="Caption">The optional public caption.</param>
/// <param name="Attribution">The optional attribution text.</param>
/// <param name="Copyright">The optional copyright notice.</param>
public sealed record CatalogTourImageUploadRequest(
    Stream Content,
    string FileName,
    string ContentType,
    string AltText,
    string? Caption = null,
    string? Attribution = null,
    string? Copyright = null)
{
    /// <summary>
    /// Initializes an upload with an authoritative declared length for transport progress.
    /// </summary>
    public CatalogTourImageUploadRequest(
        Stream Content,
        long ContentLength,
        string FileName,
        string ContentType,
        string AltText,
        string? Caption = null,
        string? Attribution = null,
        string? Copyright = null)
        : this(Content, FileName, ContentType, AltText, Caption, Attribution, Copyright)
    {
        this.ContentLength = ContentLength;
    }

    /// <summary>
    /// Gets the declared image length, or zero when an existing caller supplies an unknown-length stream.
    /// </summary>
    public long ContentLength { get; init; } = Content is { CanSeek: true } ? Content.Length - Content.Position : 0;
}
