namespace ViajantesTurismo.Catalog.Application.Tours;

/// <summary>
/// Represents a conflict with an existing Catalog tour's public slug.
/// </summary>
public sealed class CatalogTourSlugConflictException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CatalogTourSlugConflictException" /> class.
    /// </summary>
    public CatalogTourSlugConflictException()
        : base("The public tour slug is already in use.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CatalogTourSlugConflictException" /> class.
    /// </summary>
    /// <param name="message">The conflict message.</param>
    public CatalogTourSlugConflictException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CatalogTourSlugConflictException" /> class.
    /// </summary>
    /// <param name="message">The conflict message.</param>
    /// <param name="innerException">The persistence exception that caused the conflict.</param>
    public CatalogTourSlugConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
