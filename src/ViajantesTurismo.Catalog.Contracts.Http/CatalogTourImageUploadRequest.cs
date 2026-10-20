namespace ViajantesTurismo.Catalog.Contracts.Http;

/// <summary>
/// Describes a catalog tour image uploaded through the management API.
/// </summary>
/// <param name="Content">The image content.</param>
/// <param name="ContentLength">The authoritative declared image length.</param>
/// <param name="FileName">The client-supplied filename used only for validation.</param>
/// <param name="ContentType">The client-supplied media type used only for validation.</param>
/// <param name="AltText">The editor-provided accessible description.</param>
/// <param name="Caption">The optional public caption.</param>
/// <param name="Attribution">The optional attribution text.</param>
/// <param name="Copyright">The optional copyright notice.</param>
public sealed record CatalogTourImageUploadRequest(
    Stream Content,
    long ContentLength,
    string FileName,
    string ContentType,
    string AltText,
    string? Caption = null,
    string? Attribution = null,
    string? Copyright = null)
{
    /// <summary>Initializes an upload without transport progress.</summary>
    public CatalogTourImageUploadRequest(Stream content, string fileName, string contentType, string altText, string? caption = null, string? attribution = null, string? copyright = null)
        : this(content, content is null ? throw new ArgumentNullException(nameof(content)) : content.CanSeek ? content.Length - content.Position : 0, fileName, contentType, altText, caption, attribution, copyright)
    {
    }
}
