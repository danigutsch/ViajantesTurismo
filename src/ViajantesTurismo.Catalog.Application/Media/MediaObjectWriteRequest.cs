namespace ViajantesTurismo.Catalog.Application.Media;

/// <summary>
/// Describes a media object write.
/// </summary>
/// <param name="ObjectKey">The application-owned object key.</param>
/// <param name="Content">The object content stream.</param>
/// <param name="ContentType">The media content type.</param>
/// <param name="Length">The object length in bytes.</param>
/// <param name="Checksum">The optional content checksum.</param>
/// <param name="Metadata">The optional object metadata.</param>
public sealed record MediaObjectWriteRequest(
    string ObjectKey,
    Stream Content,
    string ContentType,
    long Length,
    string? Checksum = null,
    IReadOnlyDictionary<string, string>? Metadata = null);
