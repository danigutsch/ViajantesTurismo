using ViajantesTurismo.Catalog.Domain.Media;

namespace ViajantesTurismo.Catalog.Application.Media;

/// <summary>
/// Stores public media image metadata for Catalog tour galleries.
/// </summary>
public interface IPublicMediaImageStore
{
    /// <summary>
    /// Inserts or updates public media image metadata.
    /// </summary>
    /// <param name="image">The public media image metadata.</param>
    /// <param name="ct">The cancellation token.</param>
    ValueTask Upsert(PublicMediaImage image, CancellationToken ct);

    /// <summary>
    /// Gets public media image metadata by stable identifier.
    /// </summary>
    /// <param name="imageId">The stable media image identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The media image when one exists; otherwise, <see langword="null" />.</returns>
    ValueTask<PublicMediaImage?> GetImage(Guid imageId, CancellationToken ct);

    /// <summary>
    /// Lists public media images linked to a Catalog tour.
    /// </summary>
    /// <param name="catalogTourId">The Catalog tour identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The linked media images ordered by the store implementation.</returns>
    ValueTask<IReadOnlyList<PublicMediaImage>> ListByTour(Guid catalogTourId, CancellationToken ct);
}
