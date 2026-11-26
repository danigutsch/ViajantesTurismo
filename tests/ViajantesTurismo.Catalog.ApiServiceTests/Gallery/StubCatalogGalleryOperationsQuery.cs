using ViajantesTurismo.Catalog.Application.Gallery;
using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Gallery;

/// <summary>Records an operations query and supplies the selected placement result.</summary>
internal sealed class StubCatalogGalleryOperationsQuery(
    GetCatalogGalleryPlacementStatusDto? result, GetCatalogGalleryRetryHealthDto? retryHealth = null) : ICatalogGalleryOperationsQuery
{
    /// <summary>Gets the exact tour and placement passed by the endpoint.</summary>
    internal (Guid TourId, long Sequence)? Request
    {
        get;
        private set;
    }

    /// <summary>Gets the original wake passed by the reverse lookup endpoint.</summary>
    internal Guid? OriginalWakeRequest
    {
        get;
        private set;
    }

    /// <summary>Gets the server observation time passed by the health endpoint.</summary>
    internal DateTimeOffset? RetryHealthRequest
    {
        get;
        private set;
    }

    /// <inheritdoc />
    public ValueTask<GetCatalogGalleryPlacementStatusDto?> Read(Guid catalogTourId, long sequence, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Request = (catalogTourId, sequence);
        return ValueTask.FromResult(result);
    }

    /// <inheritdoc />
    public ValueTask<GetCatalogGalleryPlacementStatusDto?> ReadByOriginalWake(Guid eventId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        OriginalWakeRequest = eventId;
        return ValueTask.FromResult(result);
    }

    /// <inheritdoc />
    public ValueTask<GetCatalogGalleryRetryHealthDto> ReadRetryHealth(DateTimeOffset observedAt, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        RetryHealthRequest = observedAt;
        return ValueTask.FromResult(retryHealth ?? new GetCatalogGalleryRetryHealthDto(0, null));
    }
}
