namespace ViajantesTurismo.Catalog.Application.Media;

/// <summary>
/// Represents media upload malware scan status.
/// </summary>
public enum MediaUploadScanStatus
{
    /// <summary>
    /// The scanner is disabled for this environment.
    /// </summary>
    Disabled,

    /// <summary>
    /// The upload is waiting for a scan result.
    /// </summary>
    Pending,

    /// <summary>
    /// The upload passed scanning.
    /// </summary>
    Passed,

    /// <summary>
    /// The scanner failed before a decision was made.
    /// </summary>
    Failed,

    /// <summary>
    /// The upload was rejected by scanning.
    /// </summary>
    Rejected
}
