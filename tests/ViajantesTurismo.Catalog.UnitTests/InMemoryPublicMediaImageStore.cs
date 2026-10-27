using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Domain.Media;

namespace ViajantesTurismo.Catalog.UnitTests;

/// <summary>Single-image in-memory store that keeps the latest upserted image.</summary>
/// <param name="image">The initial stored image.</param>
internal sealed class InMemoryPublicMediaImageStore(PublicMediaImage image) : IPublicMediaImageStore
{
    /// <summary>Gets the image most recently stored.</summary>
    public PublicMediaImage Current { get; private set; } = image;

    /// <summary>Replaces the current image.</summary>
    public ValueTask Upsert(PublicMediaImage image, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Current = image;

        return ValueTask.CompletedTask;
    }

    /// <summary>Returns the current image when its identifier matches, otherwise <see langword="null" />.</summary>
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

    /// <summary>Returns the source and responsive variant object keys of the current image.</summary>
    public ValueTask<IReadOnlyList<string>> ListReferencedObjectKeys(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return ValueTask.FromResult<IReadOnlyList<string>>(
            [Current.SourceObjectKey, .. Current.ResponsiveVariants.Select(variant => variant.ObjectKey)]);
    }
}
