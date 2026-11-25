namespace ViajantesTurismo.Catalog.Application.Media;

/// <summary>Reads placement counts and serializes uploads that share a private content and editorial fingerprint.</summary>
public interface IMediaImageUploadStore
{
    /// <summary>Counts the images already linked to the tour for upload placement.</summary>
    /// <param name="catalogTourId">The Catalog tour identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The count of images linked to the tour.</returns>
    ValueTask<int> CountByTour(Guid catalogTourId, CancellationToken ct);

    /// <summary>Reserves a fingerprint until its lease is released.</summary>
    /// <param name="fingerprint">The private, versioned upload fingerprint.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A lease containing any matching image.</returns>
    ValueTask<IMediaImageUploadLease> Reserve(string fingerprint, CancellationToken ct);
}
