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

    public ValueTask<IReadOnlyList<CatalogTourDraftReadModel>> ListTours(CancellationToken ct)
    {
        IReadOnlyList<CatalogTourDraftReadModel> snapshot = drafts.ToArray();
        return ValueTask.FromResult(snapshot);
    }

    public ValueTask<CatalogTourDraftReadModel?> GetTour(Guid catalogTourId, CancellationToken ct)
    {
        return ValueTask.FromResult(drafts.FirstOrDefault(tour => tour.CatalogTourId == catalogTourId));
    }

    public ValueTask<CatalogTourDraftReadModel?> UpdatePresentation(Guid catalogTourId, CatalogTourPresentationUpdate update, CancellationToken ct)
    {
        throw new NotSupportedException();
    }

    public ValueTask<CatalogTourDraftReadModel?> GetPublishedTourBySlug(string slug, CancellationToken ct)
    {
        return ValueTask.FromResult<CatalogTourDraftReadModel?>(null);
    }
}
