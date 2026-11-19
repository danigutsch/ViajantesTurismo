using ViajantesTurismo.Catalog.Domain.Media;

namespace ViajantesTurismo.Catalog.Application.Media;

/// <summary>Holds exclusive access to the upload fingerprint until completion or disposal.</summary>
public interface IMediaImageUploadLease : IAsyncDisposable
{
    /// <summary>Gets an existing image whose content, editorial attributes, and gallery scope match.</summary>
    PublicMediaImage? ExistingImage
    {
        get;
    }

    /// <summary>Commits a new image with the upload fingerprint and any staged integration events.</summary>
    /// <param name="image">The new image.</param>
    /// <param name="ct">The cancellation token.</param>
    ValueTask Save(PublicMediaImage image, CancellationToken ct);
}
