using System.Collections.Concurrent;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Domain.Media;

namespace ViajantesTurismo.Catalog.Testing.Infrastructure;

public sealed class TestPublicMediaImageStore : IPublicMediaImageStore
{
    private readonly ConcurrentDictionary<Guid, PublicMediaImage> imagesById = new();

    private int getImageCallCount;
    private int listByTourCallCount;

    public Exception? UpsertException
    {
        get; set;
    }

    public Exception? ListByTourException
    {
        get; set;
    }

    public int GetImageCallCount => getImageCallCount;

    /// <summary>Gets the number of full per-tour image reads.</summary>
    public int ListByTourCallCount => Volatile.Read(ref listByTourCallCount);

    public ValueTask Upsert(PublicMediaImage image, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(image);
        ct.ThrowIfCancellationRequested();

        if (UpsertException is not null)
        {
            throw UpsertException;
        }

        imagesById[image.Id] = image;
        return ValueTask.CompletedTask;
    }

    public ValueTask<PublicMediaImage?> GetImage(Guid imageId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Interlocked.Increment(ref getImageCallCount);
        imagesById.TryGetValue(imageId, out var image);

        return ValueTask.FromResult(image);
    }

    public ValueTask<IReadOnlyList<PublicMediaImage>> ListByTour(Guid catalogTourId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Interlocked.Increment(ref listByTourCallCount);
        if (ListByTourException is not null)
        {
            throw ListByTourException;
        }

        return ValueTask.FromResult(ListByTour(catalogTourId));
    }

    /// <summary>Counts the test images linked to the tour.</summary>
    public int CountByTour(Guid catalogTourId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return imagesById.Count(entry => entry.Value.BelongsToTour(catalogTourId));
    }

    public ValueTask<PublicMediaImage?> GetNextPendingReviewByTour(Guid catalogTourId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var image = ListByTour(catalogTourId).FirstOrDefault(image =>
            image.RequiresHumanReview || image.AccessibilityTexts.Any(text => text.RequiresHumanReview));
        return ValueTask.FromResult(image);
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
                ListByTour);

        return ValueTask.FromResult<IReadOnlyDictionary<Guid, IReadOnlyList<PublicMediaImage>>>(result);
    }

    public ValueTask<IReadOnlyList<string>> ListReferencedObjectKeys(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var keys = imagesById.Values
            .SelectMany(image => image.ResponsiveVariants
                .Select(variant => variant.ObjectKey)
                .Prepend(image.SourceObjectKey))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        return ValueTask.FromResult<IReadOnlyList<string>>(keys);
    }

    private IReadOnlyList<PublicMediaImage> ListByTour(Guid catalogTourId)
    {
        return
        [
            .. imagesById.Values
                .Where(image => image.BelongsToTour(catalogTourId))
                .OrderByDescending(image => image.IsCoverForTour(catalogTourId))
                .ThenBy(image => image.GetDisplayOrderForTour(catalogTourId))
                .ThenBy(image => image.Id)
        ];
    }
}
