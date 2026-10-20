using SharedKernel.HttpClients;
using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.Contracts.Http;

/// <summary>
/// HTTP client contract for catalog tour management endpoints.
/// </summary>
public interface ICatalogToursApiClient
{
    /// <summary>
    /// Gets catalog tours.
    /// </summary>
    Task<HttpOutcome<CatalogTourDto[]>> GetTours(CancellationToken ct);

    /// <summary>
    /// Gets a catalog tour by identifier.
    /// </summary>
    Task<HttpOutcome<CatalogTourDto>> GetTour(Guid id, CancellationToken ct);

    /// <summary>
    /// Updates catalog tour presentation data.
    /// </summary>
    Task<HttpOutcome<CatalogTourDto>> UpdatePresentation(Guid id, UpsertCatalogTourPresentationRequest request, CancellationToken ct);

    /// <summary>
    /// Publishes a Catalog tour explicitly.
    /// </summary>
    /// <param name="id">The Catalog tour identifier.</param>
    /// <param name="request">The optimistic publication transition request.</param>
    /// <param name="ct">The cancellation token.</param>
    Task<HttpOutcome> Publish(Guid id, CatalogTourPublicationRequest request, CancellationToken ct);

    /// <summary>
    /// Unpublishes a Catalog tour explicitly.
    /// </summary>
    /// <param name="id">The Catalog tour identifier.</param>
    /// <param name="request">The optimistic publication transition request.</param>
    /// <param name="ct">The cancellation token.</param>
    Task<HttpOutcome> Unpublish(Guid id, CatalogTourPublicationRequest request, CancellationToken ct);

    /// <summary>
    /// Generates AI-assisted draft accessibility text for a media image.
    /// </summary>
    Task<HttpOutcome<CatalogMediaImageDto>> GenerateMediaImageAccessibilityDraft(Guid id, PublicMediaImageAccessibilityDraftRequest request, CancellationToken ct);

    /// <summary>
    /// Uploads a new image for a catalog tour.
    /// </summary>
    Task<HttpOutcome<CatalogMediaImageDto>> UploadTourImage(Guid id, CatalogTourImageUploadRequest request, IProgress<long>? progress, CancellationToken ct)
    {
        return UploadTourImage(id, request, ct);
    }

    /// <summary>
    /// Gets the media images linked to a catalog tour.
    /// </summary>
    Task<HttpOutcome<CatalogMediaImageDto[]>> GetTourImages(Guid id, CancellationToken ct);

    /// <summary>
    /// Approves media image accessibility text.
    /// </summary>
    Task<HttpOutcome<CatalogMediaImageDto>> ReviewMediaImageAccessibility(Guid id, PublicMediaImageAccessibilityReviewRequest request, CancellationToken ct);

    /// <summary>
    /// Gets an authenticated media preview stream.
    /// </summary>
    Task<PublicMediaObjectResponse?> GetMediaPreview(Guid id, int width, string format, CancellationToken ct);

    /// <summary>
    /// Uploads a new image for a catalog tour without reporting upload progress.
    /// </summary>
    /// <param name="id">The Catalog tour identifier.</param>
    /// <param name="request">The image content and upload metadata.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The uploaded image or the classified HTTP failure.</returns>
    Task<HttpOutcome<CatalogMediaImageDto>> UploadTourImage(Guid id, CatalogTourImageUploadRequest request, CancellationToken ct);
}
