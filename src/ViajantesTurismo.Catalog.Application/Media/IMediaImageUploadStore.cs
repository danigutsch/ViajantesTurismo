namespace ViajantesTurismo.Catalog.Application.Media;

/// <summary>Serializes uploads that share a private content and editorial fingerprint.</summary>
public interface IMediaImageUploadStore
{
    /// <summary>Reserves a fingerprint until its lease is released.</summary>
    /// <param name="fingerprint">The private, versioned upload fingerprint.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A lease containing any matching image.</returns>
    ValueTask<IMediaImageUploadLease> Reserve(string fingerprint, CancellationToken ct);
}
