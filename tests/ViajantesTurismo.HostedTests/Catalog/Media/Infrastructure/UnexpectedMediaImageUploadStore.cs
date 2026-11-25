using ViajantesTurismo.Catalog.Application.Media;

namespace ViajantesTurismo.HostedTests.Catalog.Media.Infrastructure;

/// <summary>Fails the scanner-outage case if intake reaches the image metadata store.</summary>
internal sealed class UnexpectedMediaImageUploadStore : IMediaImageUploadStore
{
    /// <inheritdoc />
    public ValueTask<int> CountByTour(Guid catalogTourId, CancellationToken ct) =>
        throw new InvalidOperationException("Image metadata is not expected when scanning fails.");

    /// <inheritdoc />
    public ValueTask<IMediaImageUploadLease> Reserve(string fingerprint, CancellationToken ct) =>
        throw new InvalidOperationException("Image metadata is not expected when scanning fails.");
}
