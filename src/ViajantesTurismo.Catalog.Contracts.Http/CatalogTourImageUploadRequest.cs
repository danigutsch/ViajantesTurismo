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
    string? Copyright = null);
