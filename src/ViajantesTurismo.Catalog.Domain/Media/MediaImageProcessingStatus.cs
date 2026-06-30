namespace ViajantesTurismo.Catalog.Domain.Media;

/// <summary>
/// Processing state for a media image.
/// </summary>
public enum MediaImageProcessingStatus
{
    /// <summary>
    /// The media image has no recorded processing state.
    /// </summary>
    None = 0,

    /// <summary>
    /// The media image is waiting for external processing.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// The media image is ready for public use.
    /// </summary>
    Ready = 2,

    /// <summary>
    /// The media image failed external processing.
    /// </summary>
    Failed = 3,
}
