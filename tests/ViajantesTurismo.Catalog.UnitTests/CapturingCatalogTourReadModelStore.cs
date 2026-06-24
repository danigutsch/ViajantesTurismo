using ViajantesTurismo.Catalog.Application.Tours;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class CapturingCatalogTourReadModelStore : ICatalogTourReadModelStore
{
    private readonly List<CatalogTourDraftReadModel> drafts = [];

    public CatalogTourDraftReadModel? Draft { get; private set; }

    public IReadOnlyCollection<CatalogTourDraftReadModel> Drafts => drafts;

    public ValueTask UpsertDraft(CatalogTourDraftReadModel tour, CancellationToken ct)
    {
        Draft = tour;
        drafts.Add(tour);

        return ValueTask.CompletedTask;
    }
}
