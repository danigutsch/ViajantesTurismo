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
}
