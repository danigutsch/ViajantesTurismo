using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Domain.Media;

namespace ViajantesTurismo.Catalog.IntegrationTests.Infrastructure;

internal sealed class UnexpectedPublicMediaImageStore : IPublicMediaImageStore
{
    public ValueTask Upsert(PublicMediaImage image, CancellationToken ct) => throw new InvalidOperationException("Image metadata is not expected when scanning fails.");

    public ValueTask<PublicMediaImage?> GetImage(Guid imageId, CancellationToken ct) => throw new InvalidOperationException("Image metadata is not expected when scanning fails.");

    public ValueTask<IReadOnlyList<PublicMediaImage>> ListByTour(Guid catalogTourId, CancellationToken ct) => throw new InvalidOperationException("Image metadata is not expected when scanning fails.");

    public ValueTask<IReadOnlyDictionary<Guid, IReadOnlyList<PublicMediaImage>>> ListByTours(
        IReadOnlyCollection<Guid> catalogTourIds,
        CancellationToken ct) => throw new InvalidOperationException("Image metadata is not expected when scanning fails.");

    public ValueTask<IReadOnlyList<string>> ListReferencedObjectKeys(CancellationToken ct) => throw new InvalidOperationException("Image metadata is not expected when scanning fails.");
}
