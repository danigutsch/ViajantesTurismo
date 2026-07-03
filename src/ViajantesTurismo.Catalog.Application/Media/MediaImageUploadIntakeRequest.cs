using ViajantesTurismo.Catalog.Domain.Media;

namespace ViajantesTurismo.Catalog.Application.Media;

/// <summary>
/// Describes an uploaded original image accepted through the Catalog media intake flow.
/// </summary>
/// <param name="MediaImageId">The application-owned media image identifier.</param>
/// <param name="Content">The uploaded image content.</param>
/// <param name="FileName">The client-supplied filename used only for validation.</param>
/// <param name="ContentType">The client-supplied content type used only for validation.</param>
/// <param name="Length">The client-supplied content length.</param>
/// <param name="AltText">The initial accessible image description.</param>
/// <param name="TourLinks">The tour gallery placements.</param>
/// <param name="Tags">The editorial tags.</param>
/// <param name="Caption">The optional public caption.</param>
/// <param name="Attribution">The optional attribution text.</param>
/// <param name="Copyright">The optional copyright notice.</param>
public sealed record MediaImageUploadIntakeRequest(
    Guid MediaImageId,
    Stream Content,
    string FileName,
    string ContentType,
    long Length,
    string AltText,
    IReadOnlyList<MediaImageTourLink> TourLinks,
    IReadOnlyList<string>? Tags = null,
    string? Caption = null,
    string? Attribution = null,
    string? Copyright = null);
