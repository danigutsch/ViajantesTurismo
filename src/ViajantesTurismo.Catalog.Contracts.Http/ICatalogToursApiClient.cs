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
    Task<ContractCommandOutcomeDto<CatalogTourDto[]>> GetTours(CancellationToken ct);

    /// <summary>
    /// Gets a catalog tour by identifier.
    /// </summary>
    Task<ContractCommandOutcomeDto<CatalogTourDto>> GetTour(Guid id, CancellationToken ct);

    /// <summary>
    /// Updates catalog tour presentation data.
    /// </summary>
    Task<ContractCommandOutcomeDto<CatalogTourDto>> UpdatePresentation(Guid id, UpsertCatalogTourPresentationRequest request, CancellationToken ct);

    /// <summary>
    /// Publishes a Catalog tour explicitly.
    /// </summary>
    /// <param name="id">The Catalog tour identifier.</param>
    /// <param name="request">The optimistic publication transition request.</param>
    /// <param name="ct">The cancellation token.</param>
    Task<ContractCommandOutcomeDto> Publish(Guid id, CatalogTourPublicationRequest request, CancellationToken ct);

    /// <summary>
    /// Unpublishes a Catalog tour explicitly.
    /// </summary>
    /// <param name="id">The Catalog tour identifier.</param>
    /// <param name="request">The optimistic publication transition request.</param>
    /// <param name="ct">The cancellation token.</param>
    Task<ContractCommandOutcomeDto> Unpublish(Guid id, CatalogTourPublicationRequest request, CancellationToken ct);

    /// <summary>
    /// Generates AI-assisted draft accessibility text for a media image.
    /// </summary>
    Task<ContractCommandOutcomeDto<CatalogMediaImageDto>> GenerateMediaImageAccessibilityDraft(Guid id, PublicMediaImageAccessibilityDraftRequest request, CancellationToken ct);

    /// <summary>
    /// Uploads a new image for a catalog tour.
    /// </summary>
    Task<ContractCommandOutcomeDto<CatalogMediaImageDto>> UploadTourImage(Guid id, CatalogTourImageUploadRequest request, IProgress<long>? progress, CancellationToken ct);

    /// <summary>
    /// Gets the media images linked to a catalog tour.
    /// </summary>
    Task<ContractCommandOutcomeDto<CatalogMediaImageDto[]>> GetTourImages(Guid id, CancellationToken ct);

    /// <summary>
    /// Approves media image accessibility text.
    /// </summary>
    Task<ContractCommandOutcomeDto<CatalogMediaImageDto>> ReviewMediaImageAccessibility(Guid id, PublicMediaImageAccessibilityReviewRequest request, CancellationToken ct);

    /// <summary>
    /// Gets an authenticated media preview stream.
    /// </summary>
    Task<PublicMediaObjectResponse?> GetMediaPreview(Guid id, int width, string format, CancellationToken ct);

    /// <inheritdoc />
    Task<ContractCommandOutcomeDto<CatalogMediaImageDto>> UploadTourImage(Guid id, CatalogTourImageUploadRequest request, CancellationToken ct)
    {
        return UploadTourImage(id, request, null, ct);
    }
}
