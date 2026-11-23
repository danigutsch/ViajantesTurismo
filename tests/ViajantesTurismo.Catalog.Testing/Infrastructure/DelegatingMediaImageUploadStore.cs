using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Domain.Media;

namespace ViajantesTurismo.Catalog.Testing.Infrastructure;

/// <summary>Adapts a test image store for intake tests that exercise validation and storage in isolation.</summary>
public sealed class DelegatingMediaImageUploadStore(IPublicMediaImageStore store) : IMediaImageUploadStore
{
    /// <inheritdoc />
    public ValueTask<IMediaImageUploadLease> Reserve(string fingerprint, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return ValueTask.FromResult<IMediaImageUploadLease>(new UploadLease(store));
    }

    private sealed class UploadLease(IPublicMediaImageStore store) : IMediaImageUploadLease
    {
        public PublicMediaImage? ExistingImage => null;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public ValueTask Save(PublicMediaImage image, bool? canPublishGalleryPlacement, CancellationToken ct) => store.Upsert(image, ct);
    }
}
