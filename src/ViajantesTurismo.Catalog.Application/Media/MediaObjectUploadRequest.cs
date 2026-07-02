namespace ViajantesTurismo.Catalog.Application.Media;

/// <summary>
/// Describes a direct media upload request.
/// </summary>
/// <param name="ObjectKey">The application-owned object key.</param>
/// <param name="ContentType">The expected media content type.</param>
/// <param name="Length">The expected upload length in bytes.</param>
/// <param name="ExpiresAfter">The upload ticket lifetime.</param>
public sealed record MediaObjectUploadRequest(
    string ObjectKey,
    string ContentType,
    long Length,
    TimeSpan ExpiresAfter);
