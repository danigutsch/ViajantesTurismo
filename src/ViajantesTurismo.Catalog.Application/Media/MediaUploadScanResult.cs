namespace ViajantesTurismo.Catalog.Application.Media;

/// <summary>
/// Describes a media upload scan result.
/// </summary>
/// <param name="Status">The scan status.</param>
/// <param name="Message">The optional scan message.</param>
public sealed record MediaUploadScanResult(MediaUploadScanStatus Status, string? Message = null)
{
    /// <summary>
    /// Gets a passed scan result.
    /// </summary>
    public static MediaUploadScanResult Passed { get; } = new(MediaUploadScanStatus.Passed);
}
