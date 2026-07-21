namespace ViajantesTurismo.Catalog.Domain.Tours;

/// <summary>
/// Represents an attempt to publish a Catalog tour before its required public content is complete.
/// </summary>
public sealed class CatalogTourPublicationNotReadyException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CatalogTourPublicationNotReadyException" /> class.
    /// </summary>
    public CatalogTourPublicationNotReadyException()
        : base("Catalog tours require a title, summary, and slug before publication.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CatalogTourPublicationNotReadyException" /> class.
    /// </summary>
    /// <param name="message">The validation message.</param>
    public CatalogTourPublicationNotReadyException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CatalogTourPublicationNotReadyException" /> class.
    /// </summary>
    /// <param name="message">The validation message.</param>
    /// <param name="innerException">The exception that caused publication validation to fail.</param>
    public CatalogTourPublicationNotReadyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
