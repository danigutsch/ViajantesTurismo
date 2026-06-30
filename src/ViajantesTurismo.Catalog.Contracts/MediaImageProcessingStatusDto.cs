namespace ViajantesTurismo.Catalog.Contracts;

/// <summary>
/// Processing state for public media image metadata.
/// </summary>
public enum MediaImageProcessingStatusDto
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
