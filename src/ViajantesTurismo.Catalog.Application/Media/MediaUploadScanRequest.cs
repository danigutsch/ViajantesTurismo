namespace ViajantesTurismo.Catalog.Application.Media;

/// <summary>
/// Describes a media upload scan request.
/// </summary>
/// <param name="ObjectKey">The application-owned object key.</param>
/// <param name="Content">The content to scan.</param>
/// <param name="ContentType">The media content type.</param>
/// <param name="Length">The content length in bytes.</param>
public sealed record MediaUploadScanRequest(
    string ObjectKey,
    Stream Content,
    string ContentType,
    long Length);
