using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Domain.Media;

namespace ViajantesTurismo.Catalog.UnitTests;

internal sealed class ThrowingPublicMediaImageStore(PublicMediaImage image, Exception exception) : IPublicMediaImageStore
{
    public PublicMediaImage Current { get; } = image;

    public ValueTask Upsert(PublicMediaImage image, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        throw exception;
    }

    public ValueTask<PublicMediaImage?> GetImage(Guid imageId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return ValueTask.FromResult(Current.Id == imageId ? Current : null);
    }

    public ValueTask<IReadOnlyList<PublicMediaImage>> ListByTour(Guid catalogTourId, CancellationToken ct)
        => ValueTask.FromResult<IReadOnlyList<PublicMediaImage>>([]);

    public ValueTask<IReadOnlyDictionary<Guid, IReadOnlyList<PublicMediaImage>>> ListByTours(
        IReadOnlyCollection<Guid> catalogTourIds,
        CancellationToken ct)
        => ValueTask.FromResult<IReadOnlyDictionary<Guid, IReadOnlyList<PublicMediaImage>>>(new Dictionary<Guid, IReadOnlyList<PublicMediaImage>>());
}
