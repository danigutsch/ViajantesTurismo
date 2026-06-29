using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Domain.Media;

namespace ViajantesTurismo.Catalog.UnitTests;

internal sealed class StubPublicMediaImageStore : IPublicMediaImageStore
{
    public ValueTask Upsert(PublicMediaImage image, CancellationToken ct)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask<PublicMediaImage?> GetImage(Guid imageId, CancellationToken ct)
    {
        return ValueTask.FromResult<PublicMediaImage?>(null);
    }

    public ValueTask<IReadOnlyList<PublicMediaImage>> ListByTour(Guid catalogTourId, CancellationToken ct)
    {
        return ValueTask.FromResult<IReadOnlyList<PublicMediaImage>>([]);
    }

    public ValueTask<IReadOnlyDictionary<Guid, IReadOnlyList<PublicMediaImage>>> ListByTours(
        IReadOnlyCollection<Guid> catalogTourIds,
        CancellationToken ct)
    {
        return ValueTask.FromResult<IReadOnlyDictionary<Guid, IReadOnlyList<PublicMediaImage>>>(
            catalogTourIds.Distinct().ToDictionary(tourId => tourId, _ => (IReadOnlyList<PublicMediaImage>)[]));
    }
}
