using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Domain.Media;

namespace ViajantesTurismo.Catalog.UnitTests;

/// <summary>Image store stub that accepts writes and returns no images.</summary>
internal sealed class StubPublicMediaImageStore : IPublicMediaImageStore
{
    /// <summary>Accepts the image without storing it.</summary>
    public ValueTask Upsert(PublicMediaImage image, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return ValueTask.CompletedTask;
    }

    /// <summary>Always returns <see langword="null" />.</summary>
    public ValueTask<PublicMediaImage?> GetImage(Guid imageId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return ValueTask.FromResult<PublicMediaImage?>(null);
    }

    /// <summary>Always returns an empty list.</summary>
    public ValueTask<IReadOnlyList<PublicMediaImage>> ListByTour(Guid catalogTourId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return ValueTask.FromResult<IReadOnlyList<PublicMediaImage>>([]);
    }

    /// <summary>Returns an empty image list for each distinct tour identifier.</summary>
    public ValueTask<IReadOnlyDictionary<Guid, IReadOnlyList<PublicMediaImage>>> ListByTours(
        IReadOnlyCollection<Guid> catalogTourIds,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return ValueTask.FromResult<IReadOnlyDictionary<Guid, IReadOnlyList<PublicMediaImage>>>(
            catalogTourIds.Distinct().ToDictionary(tourId => tourId, _ => (IReadOnlyList<PublicMediaImage>)[]));
    }

    /// <summary>Always returns an empty list.</summary>
    public ValueTask<IReadOnlyList<string>> ListReferencedObjectKeys(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return ValueTask.FromResult<IReadOnlyList<string>>([]);
    }
}
