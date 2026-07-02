namespace ViajantesTurismo.Catalog.Application.Media;

/// <summary>
/// Stores media objects for public website assets.
/// </summary>
public interface IMediaObjectStore
{
    /// <summary>
    /// Writes a media object to storage.
    /// </summary>
    /// <param name="request">The write request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The stored object result.</returns>
    ValueTask<MediaObjectWriteResult> Put(MediaObjectWriteRequest request, CancellationToken ct);

    /// <summary>
    /// Creates a time-limited upload ticket for direct uploads.
    /// </summary>
    /// <param name="request">The upload request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The upload ticket.</returns>
    ValueTask<MediaObjectUploadTicket> CreateUploadUrl(MediaObjectUploadRequest request, CancellationToken ct);

    /// <summary>
    /// Deletes a media object if it exists.
    /// </summary>
    /// <param name="objectKey">The application-owned object key.</param>
    /// <param name="ct">The cancellation token.</param>
    ValueTask Delete(string objectKey, CancellationToken ct);
}
