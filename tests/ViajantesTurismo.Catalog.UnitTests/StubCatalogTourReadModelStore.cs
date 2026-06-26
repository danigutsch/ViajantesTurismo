using ViajantesTurismo.Catalog.Application.Tours;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class StubCatalogTourReadModelStore(params CatalogTourDraftReadModel[] tours) : ICatalogTourReadModelStore
{
    public ValueTask UpsertDraft(CatalogTourDraftReadModel tour, CancellationToken ct)
    {
        throw new NotSupportedException();
    }

    public ValueTask<IReadOnlyList<CatalogTourDraftReadModel>> ListTours(CancellationToken ct)
    {
        IReadOnlyList<CatalogTourDraftReadModel> snapshot = tours;
        return ValueTask.FromResult(snapshot);
    }

    public ValueTask<CatalogTourDraftReadModel?> GetPublishedTourBySlug(string slug, CancellationToken ct)
    {
        return ValueTask.FromResult<CatalogTourDraftReadModel?>(null);
    }
}
