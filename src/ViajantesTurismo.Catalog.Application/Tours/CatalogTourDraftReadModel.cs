using ViajantesTurismo.Catalog.Domain.Tours;

namespace ViajantesTurismo.Catalog.Application.Tours;

/// <summary>
/// Read model row for a draft Catalog tour projection.
/// </summary>
/// <param name="CatalogTourId">The Catalog tour identifier.</param>
/// <param name="AdminTourId">The source Admin tour identifier.</param>
/// <param name="Identifier">The customer-facing tour identifier.</param>
/// <param name="Title">The customer-facing tour title.</param>
/// <param name="Slug">The public URL slug.</param>
/// <param name="IsPublished">Whether the tour is visible on the public website.</param>
/// <param name="Position">The event-store position that produced the row.</param>
/// <param name="UpdatedAt">The event-recorded timestamp that produced the row.</param>
public sealed record CatalogTourDraftReadModel(
    Guid CatalogTourId,
    Guid AdminTourId,
    string Identifier,
    string Title,
    string Slug,
    bool IsPublished,
    long Position,
    DateTimeOffset UpdatedAt)
{
    /// <summary>
    /// Gets a value indicating whether this tour can be shown on public endpoints.
    /// </summary>
    public bool IsPubliclyVisible => IsPublished;

    /// <summary>
    /// Creates a draft read model from a Catalog tour creation event.
    /// </summary>
    /// <param name="draftCreated">The source event.</param>
    /// <param name="position">The event-store position.</param>
    /// <param name="recordedAt">The event-recorded timestamp.</param>
    /// <returns>The initialized draft read model.</returns>
    public static CatalogTourDraftReadModel FromDraftCreated(
        CatalogTourDraftCreated draftCreated,
        long position,
        DateTimeOffset recordedAt)
    {
        ArgumentNullException.ThrowIfNull(draftCreated);

        return new CatalogTourDraftReadModel(
            draftCreated.CatalogTourId,
            draftCreated.AdminTourId,
            draftCreated.Identifier,
            draftCreated.Title,
            draftCreated.Identifier.Trim(),
            IsPublished: false,
            position,
            recordedAt);
    }
}
