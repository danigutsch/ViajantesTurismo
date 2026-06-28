namespace ViajantesTurismo.Catalog.Application.Tours;

/// <summary>
/// Persists Catalog tour projection rows used by management and public read models.
/// </summary>
public interface ICatalogTourReadModelStore
{
    /// <summary>
    /// Inserts or updates a draft Catalog tour read model row.
    /// </summary>
    /// <param name="tour">The projected tour row.</param>
    /// <param name="ct">The cancellation token.</param>
    ValueTask UpsertDraft(CatalogTourDraftReadModel tour, CancellationToken ct);

    /// <summary>
    /// Updates customer-facing presentation values for an existing Catalog tour.
    /// </summary>
    /// <param name="catalogTourId">The Catalog tour identifier.</param>
    /// <param name="update">The presentation update.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The updated tour when one exists; otherwise, <see langword="null" />.</returns>
    ValueTask<CatalogTourDraftReadModel?> UpdatePresentation(Guid catalogTourId, CatalogTourPresentationUpdate update, CancellationToken ct);

    /// <summary>
    /// Lists Catalog tour projection rows for management workflows.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The current Catalog tour projection rows.</returns>
    ValueTask<IReadOnlyList<CatalogTourDraftReadModel>> ListTours(CancellationToken ct);

    /// <summary>
    /// Gets a published Catalog tour by its public slug.
    /// </summary>
    /// <param name="slug">The public tour slug.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The published tour row when one exists; otherwise, <see langword="null" />.</returns>
    ValueTask<CatalogTourDraftReadModel?> GetPublishedTourBySlug(string slug, CancellationToken ct);
}
