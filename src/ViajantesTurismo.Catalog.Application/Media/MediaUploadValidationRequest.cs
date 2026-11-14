namespace ViajantesTurismo.Catalog.Application.Media;

/// <summary>
/// Describes a media upload validation request. The content itself is checked by <c>ImageSanitizer</c> during intake.
/// </summary>
/// <param name="FileName">The client-supplied filename.</param>
/// <param name="ContentType">The client-supplied content type.</param>
/// <param name="Length">The upload length in bytes.</param>
public sealed record MediaUploadValidationRequest(
    string FileName,
    string ContentType,
    long Length);
