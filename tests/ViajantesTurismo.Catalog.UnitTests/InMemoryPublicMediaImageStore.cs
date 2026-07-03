using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Domain.Media;

namespace ViajantesTurismo.Catalog.UnitTests;

internal sealed class InMemoryPublicMediaImageStore(PublicMediaImage image) : IPublicMediaImageStore
{
    public PublicMediaImage Current { get; private set; } = image;

    public ValueTask Upsert(PublicMediaImage image, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Current = image;

        return ValueTask.CompletedTask;
    }

    public ValueTask<PublicMediaImage?> GetImage(Guid imageId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return ValueTask.FromResult(Current.Id == imageId ? Current : null);
    }

    public ValueTask<IReadOnlyList<PublicMediaImage>> ListByTour(Guid catalogTourId, CancellationToken ct)
    {
        return ValueTask.FromResult<IReadOnlyList<PublicMediaImage>>([]);
    }

    public ValueTask<IReadOnlyDictionary<Guid, IReadOnlyList<PublicMediaImage>>> ListByTours(
        IReadOnlyCollection<Guid> catalogTourIds,
        CancellationToken ct)
    {
        return ValueTask.FromResult<IReadOnlyDictionary<Guid, IReadOnlyList<PublicMediaImage>>>(new Dictionary<Guid, IReadOnlyList<PublicMediaImage>>());
    }
}
