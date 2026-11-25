using ViajantesTurismo.Catalog.Domain.Tours;
using ViajantesTurismo.Catalog.Application.Tours;

namespace ViajantesTurismo.Catalog.UnitTests.Tours;

public sealed class RecordingCatalogTourReadModelStore : ICatalogTourReadModelStore
{
    private readonly List<(Guid CatalogTourId, CatalogTourPresentationUpdate Update, long StreamVersion, long Position, DateTimeOffset UpdatedAt)> presentationUpdates = [];

    private readonly List<(Guid CatalogTourId, bool IsPublished, long StreamVersion, long Position, DateTimeOffset UpdatedAt)> publicationChanges = [];

    public IReadOnlyList<(Guid CatalogTourId, CatalogTourPresentationUpdate Update, long StreamVersion, long Position, DateTimeOffset UpdatedAt)> PresentationUpdates => presentationUpdates;

    public IReadOnlyList<(Guid CatalogTourId, bool IsPublished, long StreamVersion, long Position, DateTimeOffset UpdatedAt)> PublicationChanges => publicationChanges;

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
        presentationUpdates.Add((catalogTourId, update, streamVersion, position, updatedAt));
        return ValueTask.FromResult<CatalogTourDraftReadModel?>(null);
    }

    public ValueTask<CatalogTourDraftReadModel?> SetPublicationStatus(
        Guid catalogTourId,
        bool isPublished,
        long streamVersion,
        long position,
        DateTimeOffset updatedAt,
        CancellationToken ct)
    {
        publicationChanges.Add((catalogTourId, isPublished, streamVersion, position, updatedAt));
        return ValueTask.FromResult<CatalogTourDraftReadModel?>(null);
    }

    public ValueTask<CatalogTourDraftReadModel?> GetTour(Guid catalogTourId, CancellationToken ct)
    {
        throw new NotSupportedException();
    }

    public ValueTask<IReadOnlyList<CatalogTourDraftReadModel>> ListTours(CancellationToken ct)
    {
        throw new NotSupportedException();
    }

    public ValueTask<CatalogTourDraftReadModel?> GetPublishedTourBySlug(string slug, CancellationToken ct)
    {
        throw new NotSupportedException();
    }

    public ValueTask<CatalogTourDraftReadModel?> UpdateGallery(
        CatalogTourGalleryChanged gallery, long streamVersion, long position, DateTimeOffset updatedAt, CancellationToken ct) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public ValueTask<bool> AdvanceStreamVersion(Guid catalogTourId, long streamVersion, CancellationToken ct) =>
        throw new NotSupportedException();
}
