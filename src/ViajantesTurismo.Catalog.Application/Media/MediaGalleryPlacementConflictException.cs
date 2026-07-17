namespace ViajantesTurismo.Catalog.Application.Media;

/// <summary>
/// Indicates that a concurrent write changed a tour gallery placement.
/// </summary>
public sealed class MediaGalleryPlacementConflictException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MediaGalleryPlacementConflictException"/> class.
    /// </summary>
    public MediaGalleryPlacementConflictException()
        : base("The tour gallery changed while the media image was being saved.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaGalleryPlacementConflictException"/> class.
    /// </summary>
    /// <param name="message">The conflict message.</param>
    public MediaGalleryPlacementConflictException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaGalleryPlacementConflictException"/> class.
    /// </summary>
    /// <param name="message">The conflict message.</param>
    /// <param name="innerException">The database exception that caused the conflict.</param>
    public MediaGalleryPlacementConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
