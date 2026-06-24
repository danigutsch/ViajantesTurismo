using ViajantesTurismo.Catalog.Application.Tours;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class CapturingCatalogTourReadModelStore : ICatalogTourReadModelStore
{
    public CatalogTourDraftReadModel? Draft { get; private set; }

    public ValueTask UpsertDraft(CatalogTourDraftReadModel tour, CancellationToken ct)
    {
        Draft = tour;

        return ValueTask.CompletedTask;
    }
}
