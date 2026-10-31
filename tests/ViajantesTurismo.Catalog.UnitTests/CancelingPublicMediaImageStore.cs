using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Domain.Media;

namespace ViajantesTurismo.Catalog.UnitTests;

/// <summary>Image store stub whose upsert cancels the supplied source and then throws <see cref="OperationCanceledException" />.</summary>
/// <param name="image">Image returned by <c>GetImage</c> when its identifier matches.</param>
/// <param name="cancellation">The source cancelled by <c>Upsert</c>.</param>
internal sealed class CancelingPublicMediaImageStore(PublicMediaImage image, CancellationTokenSource cancellation) : IPublicMediaImageStore
{
    /// <summary>Cancels the source, then throws the cancellation exception.</summary>
    public async ValueTask Upsert(PublicMediaImage image, CancellationToken ct)
    {
        await cancellation.CancelAsync();

        throw new OperationCanceledException(ct);
    }

    /// <summary>Returns the image when its identifier matches, otherwise <see langword="null" />.</summary>
    public ValueTask<PublicMediaImage?> GetImage(Guid imageId, CancellationToken ct) =>
        ValueTask.FromResult(image.Id == imageId ? image : null);

    /// <summary>Always returns an empty list.</summary>
    public ValueTask<IReadOnlyList<PublicMediaImage>> ListByTour(Guid catalogTourId, CancellationToken ct) =>
        ValueTask.FromResult<IReadOnlyList<PublicMediaImage>>([]);

    /// <summary>Always returns an empty dictionary.</summary>
    public ValueTask<IReadOnlyDictionary<Guid, IReadOnlyList<PublicMediaImage>>> ListByTours(
        IReadOnlyCollection<Guid> catalogTourIds,
        CancellationToken ct) =>
        ValueTask.FromResult<IReadOnlyDictionary<Guid, IReadOnlyList<PublicMediaImage>>>(new Dictionary<Guid, IReadOnlyList<PublicMediaImage>>());

    /// <summary>Always returns an empty list.</summary>
    public ValueTask<IReadOnlyList<string>> ListReferencedObjectKeys(CancellationToken ct) =>
        ValueTask.FromResult<IReadOnlyList<string>>([]);
}
