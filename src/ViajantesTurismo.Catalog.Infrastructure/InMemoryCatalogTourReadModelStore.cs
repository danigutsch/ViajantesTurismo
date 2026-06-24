using System.Collections.Concurrent;
using ViajantesTurismo.Catalog.Application.Tours;

namespace ViajantesTurismo.Catalog.Infrastructure;

/// <summary>
/// Stores Catalog tour projections in memory for the current service instance.
/// </summary>
/// <remarks>
/// This is process-local foundation storage until the persistent Catalog read model store is added.
/// </remarks>
public sealed class InMemoryCatalogTourReadModelStore : ICatalogTourReadModelStore
{
    private readonly ConcurrentDictionary<Guid, CatalogTourDraftReadModel> tours = [];

    /// <inheritdoc />
    public ValueTask UpsertDraft(CatalogTourDraftReadModel tour, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(tour);

        ct.ThrowIfCancellationRequested();
        tours.AddOrUpdate(tour.CatalogTourId, tour, (_, _) => tour);

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<CatalogTourDraftReadModel>> ListTours(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        IReadOnlyList<CatalogTourDraftReadModel> snapshot = tours.Values
            .OrderBy(tour => tour.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(tour => tour.CatalogTourId)
            .ToArray();

        return ValueTask.FromResult(snapshot);
    }

    /// <inheritdoc />
    public ValueTask<CatalogTourDraftReadModel?> GetPublishedTourBySlug(string slug, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        ct.ThrowIfCancellationRequested();

        return ValueTask.FromResult<CatalogTourDraftReadModel?>(null);
    }
}
