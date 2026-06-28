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
    DateTimeOffset UpdatedAt);
