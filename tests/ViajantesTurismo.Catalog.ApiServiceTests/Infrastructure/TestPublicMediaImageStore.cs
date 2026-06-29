using System.Collections.Concurrent;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Domain.Media;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure;

internal sealed class TestPublicMediaImageStore : IPublicMediaImageStore
{
    private readonly ConcurrentDictionary<Guid, PublicMediaImage> imagesById = new();

    public ValueTask Upsert(PublicMediaImage image, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(image);
        ct.ThrowIfCancellationRequested();

        imagesById[image.Id] = image;
        return ValueTask.CompletedTask;
    }

    public ValueTask<PublicMediaImage?> GetImage(Guid imageId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        imagesById.TryGetValue(imageId, out var image);

        return ValueTask.FromResult(image);
    }

    public ValueTask<IReadOnlyList<PublicMediaImage>> ListByTour(Guid catalogTourId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return ValueTask.FromResult(ListByTour(catalogTourId));
    }

    public ValueTask<IReadOnlyDictionary<Guid, IReadOnlyList<PublicMediaImage>>> ListByTours(
        IReadOnlyCollection<Guid> catalogTourIds,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var result = catalogTourIds
            .Distinct()
            .ToDictionary(
                tourId => tourId,
                tourId => ListByTour(tourId));

        return ValueTask.FromResult<IReadOnlyDictionary<Guid, IReadOnlyList<PublicMediaImage>>>(result);
    }

    private IReadOnlyList<PublicMediaImage> ListByTour(Guid catalogTourId)
    {
        return
        [
            .. imagesById.Values
                .Where(image => image.TourLinks.Any(link => link.CatalogTourId == catalogTourId))
                .OrderByDescending(image => image.TourLinks.Any(link => link.CatalogTourId == catalogTourId && link.IsCover))
                .ThenBy(image => image.TourLinks.Where(link => link.CatalogTourId == catalogTourId).Min(link => link.DisplayOrder))
                .ThenBy(image => image.Id)
        ];
    }
}
