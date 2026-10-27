using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Domain.Media;

namespace ViajantesTurismo.Catalog.IntegrationTests.Infrastructure;

/// <summary>Image store fake that fails the test on any use.</summary>
internal sealed class UnexpectedPublicMediaImageStore : IPublicMediaImageStore
{
    /// <summary>Always throws because no image metadata is expected when scanning fails.</summary>
    public ValueTask Upsert(PublicMediaImage image, CancellationToken ct) => throw new InvalidOperationException("Image metadata is not expected when scanning fails.");

    /// <summary>Always throws because no image metadata is expected when scanning fails.</summary>
    public ValueTask<PublicMediaImage?> GetImage(Guid imageId, CancellationToken ct) => throw new InvalidOperationException("Image metadata is not expected when scanning fails.");

    /// <summary>Always throws because no image metadata is expected when scanning fails.</summary>
    public ValueTask<IReadOnlyList<PublicMediaImage>> ListByTour(Guid catalogTourId, CancellationToken ct) => throw new InvalidOperationException("Image metadata is not expected when scanning fails.");

    /// <summary>Always throws because no image metadata is expected when scanning fails.</summary>
    public ValueTask<IReadOnlyDictionary<Guid, IReadOnlyList<PublicMediaImage>>> ListByTours(
        IReadOnlyCollection<Guid> catalogTourIds,
        CancellationToken ct) => throw new InvalidOperationException("Image metadata is not expected when scanning fails.");

    /// <summary>Always throws because no image metadata is expected when scanning fails.</summary>
    public ValueTask<IReadOnlyList<string>> ListReferencedObjectKeys(CancellationToken ct) => throw new InvalidOperationException("Image metadata is not expected when scanning fails.");
}
