namespace ViajantesTurismo.Catalog.Application.Media;

/// <summary>
/// Describes a media upload validation request.
/// </summary>
/// <param name="FileName">The client-supplied filename.</param>
/// <param name="ContentType">The client-supplied content type.</param>
/// <param name="Length">The upload length in bytes.</param>
/// <param name="HeaderBytes">The first bytes of the uploaded file.</param>
public sealed record MediaUploadValidationRequest(
    string FileName,
    string ContentType,
    long Length,
    ReadOnlyMemory<byte> HeaderBytes);
