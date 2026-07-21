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
    /// Gets the concise customer-facing summary.
    /// </summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>
    /// Gets the detailed customer-facing description.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets the plain-text customer-facing itinerary.
    /// </summary>
    public string Itinerary { get; init; } = string.Empty;

    /// <summary>
    /// Gets the optional search-engine title override.
    /// </summary>
    public string SeoTitle { get; init; } = string.Empty;

    /// <summary>
    /// Gets the optional search-engine description override.
    /// </summary>
    public string SeoDescription { get; init; } = string.Empty;

    /// <summary>
    /// Gets the event-stream version represented by this read model.
    /// </summary>
    public long StreamVersion { get; init; } = 1;

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
            CatalogTourSlug.RequireCanonical(draftCreated.InitialSlug),
            IsPublished: false,
            position,
            recordedAt)
        {
            StreamVersion = 1
        };
    }
}
