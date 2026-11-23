using ViajantesTurismo.Catalog.Domain.Tours;
using ViajantesTurismo.Catalog.Application.Tours;
using PandaBear.InputNormalization;

namespace ViajantesTurismo.Catalog.UnitTests.Tours;

public sealed class StubCatalogTourReadModelStore(params CatalogTourDraftReadModel[] tours) : ICatalogTourReadModelStore
{
    public ValueTask UpsertDraft(CatalogTourDraftReadModel tour, CancellationToken ct)
    {
        throw new NotSupportedException();
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

    public ValueTask<CatalogTourDraftReadModel?> GetTour(Guid catalogTourId, CancellationToken ct)
    {
        var tour = tours.FirstOrDefault(tour => tour.CatalogTourId == catalogTourId);
        return ValueTask.FromResult(tour);
    }

    public ValueTask<IReadOnlyList<CatalogTourDraftReadModel>> ListTours(CancellationToken ct)
    {
        IReadOnlyList<CatalogTourDraftReadModel> snapshot = tours;
        return ValueTask.FromResult(snapshot);
    }

    public ValueTask<CatalogTourDraftReadModel?> GetPublishedTourBySlug(string slug, CancellationToken ct)
    {
        var sanitizedSlug = StringSanitizer.Sanitize(slug) ?? string.Empty;
        var tour = tours.FirstOrDefault(tour =>
            tour.IsPublished && string.Equals(tour.Slug, sanitizedSlug, StringComparison.Ordinal));
        return ValueTask.FromResult(tour);
    }

    public ValueTask<CatalogTourDraftReadModel?> UpdateGallery(
        CatalogTourGalleryChanged gallery, long streamVersion, long position, DateTimeOffset updatedAt, CancellationToken ct) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public ValueTask<bool> AdvanceStreamVersion(Guid catalogTourId, long streamVersion, CancellationToken ct) =>
        throw new NotSupportedException();
}
