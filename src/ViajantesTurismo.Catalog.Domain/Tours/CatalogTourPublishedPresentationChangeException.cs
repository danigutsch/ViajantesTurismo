namespace ViajantesTurismo.Catalog.Domain.Tours;

/// <summary>
/// Indicates that a published tour must be unpublished before its presentation can change.
/// </summary>
public sealed class CatalogTourPublishedPresentationChangeException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CatalogTourPublishedPresentationChangeException" /> class.
    /// </summary>
    public CatalogTourPublishedPresentationChangeException()
        : base("Published tours must be unpublished before their presentation can change.")
    {
    }

    /// <summary>
    /// Initializes a new instance with a custom message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public CatalogTourPublishedPresentationChangeException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance with a custom message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that caused this failure.</param>
    public CatalogTourPublishedPresentationChangeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
