using ViajantesTurismo.Catalog.Application.Tours;

namespace ViajantesTurismo.Catalog.UnitTests.Tours;

public sealed class CapturingCatalogTourReadModelStore : ICatalogTourReadModelStore
{
    private readonly List<CatalogTourDraftReadModel> drafts = [];

    public CatalogTourDraftReadModel? Draft
    {
        get; private set;
    }

    public IReadOnlyCollection<CatalogTourDraftReadModel> Drafts => drafts;

    /// <summary>Runs before the write so a test can simulate a failed store operation.</summary>
    internal Action<CancellationToken>? BeforeDraftUpsert
    {
        get; set;
    }

    public ValueTask UpsertDraft(CatalogTourDraftReadModel tour, CancellationToken ct)
    {
        BeforeDraftUpsert?.Invoke(ct);
        Draft = tour;
        drafts.Add(tour);

        return ValueTask.CompletedTask;
    }

    public ValueTask<IReadOnlyList<CatalogTourDraftReadModel>> ListTours(CancellationToken ct)
    {
        IReadOnlyList<CatalogTourDraftReadModel> snapshot = [.. drafts];
        return ValueTask.FromResult(snapshot);
    }

    public ValueTask<CatalogTourDraftReadModel?> GetTour(Guid catalogTourId, CancellationToken ct)
    {
        return ValueTask.FromResult(drafts.FirstOrDefault(tour => tour.CatalogTourId == catalogTourId));
    }

    public ValueTask<CatalogTourDraftReadModel?> UpdatePresentation(
        Guid catalogTourId,
        CatalogTourPresentationUpdate update,
        long streamVersion,
        long position,
        DateTimeOffset updatedAt,
        CancellationToken ct)
    {
        throw new NotSupportedException();
    }

    public ValueTask<CatalogTourDraftReadModel?> SetPublicationStatus(
        Guid catalogTourId,
        bool isPublished,
        long streamVersion,
        long position,
        DateTimeOffset updatedAt,
        CancellationToken ct)
    {
        throw new NotSupportedException();
    }

    public ValueTask<CatalogTourDraftReadModel?> GetPublishedTourBySlug(string slug, CancellationToken ct)
    {
        return ValueTask.FromResult<CatalogTourDraftReadModel?>(null);
    }
}
