using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Domain.Media;

namespace ViajantesTurismo.Catalog.UnitTests;

/// <summary>Image store stub whose upsert throws the configured exception.</summary>
/// <param name="image">Image returned by <c>GetImage</c> when its identifier matches.</param>
/// <param name="exception">Exception thrown by <c>Upsert</c>.</param>
internal sealed class ThrowingPublicMediaImageStore(PublicMediaImage image, Exception exception) : IPublicMediaImageStore
{
    /// <summary>Gets the image served by this store.</summary>
    public PublicMediaImage Current { get; } = image;

    /// <summary>Always throws the configured exception.</summary>
    public ValueTask Upsert(PublicMediaImage image, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        throw exception;
    }

    /// <summary>Returns the image when its identifier matches, otherwise <see langword="null" />.</summary>
    public ValueTask<PublicMediaImage?> GetImage(Guid imageId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return ValueTask.FromResult(Current.Id == imageId ? Current : null);
    }

    /// <summary>Always returns an empty list.</summary>
    public ValueTask<IReadOnlyList<PublicMediaImage>> ListByTour(Guid catalogTourId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return ValueTask.FromResult<IReadOnlyList<PublicMediaImage>>([]);
    }

    /// <summary>Always returns an empty dictionary.</summary>
    public ValueTask<IReadOnlyDictionary<Guid, IReadOnlyList<PublicMediaImage>>> ListByTours(
        IReadOnlyCollection<Guid> catalogTourIds,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return ValueTask.FromResult<IReadOnlyDictionary<Guid, IReadOnlyList<PublicMediaImage>>>(new Dictionary<Guid, IReadOnlyList<PublicMediaImage>>());
    }

    /// <summary>Always returns an empty list.</summary>
    public ValueTask<IReadOnlyList<string>> ListReferencedObjectKeys(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return ValueTask.FromResult<IReadOnlyList<string>>([]);
    }
}
