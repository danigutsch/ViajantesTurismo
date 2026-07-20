namespace ViajantesTurismo.Catalog.Application.Tours;

/// <summary>
/// Indicates that a committed Catalog tour event is waiting for projection replay.
/// </summary>
public sealed class CatalogTourProjectionPendingException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CatalogTourProjectionPendingException" /> class.
    /// </summary>
    public CatalogTourProjectionPendingException()
        : base("The Catalog tour change was committed and is waiting for projection.")
    {
    }

    /// <summary>
    /// Initializes a new instance with a custom message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public CatalogTourProjectionPendingException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance with a custom message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The projection failure.</param>
    public CatalogTourProjectionPendingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
