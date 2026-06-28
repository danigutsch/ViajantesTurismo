using ViajantesTurismo.Catalog.Application.Tours;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure;

internal sealed class TestCatalogTourReadModelStore : ICatalogTourReadModelStore
{
    public ValueTask UpsertDraft(CatalogTourDraftReadModel tour, CancellationToken ct)
    {
        throw new NotSupportedException();
    }

    public ValueTask<CatalogTourDraftReadModel?> UpdatePresentation(Guid catalogTourId, CatalogTourPresentationUpdate update, CancellationToken ct)
    {
        throw new NotSupportedException();
    }

    public ValueTask<IReadOnlyList<CatalogTourDraftReadModel>> ListTours(CancellationToken ct)
    {
        IReadOnlyList<CatalogTourDraftReadModel> tours = [];
        return ValueTask.FromResult(tours);
    }

    public ValueTask<CatalogTourDraftReadModel?> GetPublishedTourBySlug(string slug, CancellationToken ct)
    {
        return ValueTask.FromResult<CatalogTourDraftReadModel?>(null);
    }
}
