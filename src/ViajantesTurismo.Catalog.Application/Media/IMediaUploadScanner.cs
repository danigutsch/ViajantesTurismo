namespace ViajantesTurismo.Catalog.Application.Media;

/// <summary>
/// Scans uploaded media before publication.
/// </summary>
public interface IMediaUploadScanner
{
    /// <summary>
    /// Scans uploaded media content.
    /// </summary>
    /// <param name="request">The scan request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The scan result.</returns>
    ValueTask<MediaUploadScanResult> Scan(MediaUploadScanRequest request, CancellationToken ct);
}
