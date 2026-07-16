using System.Net;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Contracts.Http;

namespace ViajantesTurismo.Admin.SystemTests.Infrastructure.Helpers;

internal sealed class TransientCatalogToursApiClient(
    CatalogTourDto tour,
    HttpStatusCode initialFailureStatusCode = HttpStatusCode.InternalServerError) : ICatalogToursApiClient
{
    public int GetToursCallCount { get; private set; }

    public Task<CatalogTourDto[]> GetTours(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        GetToursCallCount++;
        return GetToursCallCount == 1
            ? Task.FromException<CatalogTourDto[]>(new HttpRequestException(
                "Catalog is temporarily unavailable.",
                null,
                initialFailureStatusCode))
            : Task.FromResult<CatalogTourDto[]>([tour]);
    }

    public Task<CatalogTourDto?> GetTour(Guid id, CancellationToken ct)
    {
        return Task.FromResult<CatalogTourDto?>(null);
    }

    public Task<CatalogTourDto?> UpdatePresentation(Guid id, UpsertCatalogTourPresentationRequest request, CancellationToken ct)
    {
        return Task.FromResult<CatalogTourDto?>(null);
    }

    public Task<CatalogMediaImageDto?> GenerateMediaImageAccessibilityDraft(
        Guid id,
        PublicMediaImageAccessibilityDraftRequest request,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return Task.FromResult<CatalogMediaImageDto?>(null);
    }

    public Task<CatalogMediaImageDto?> UploadTourImage(Guid id, CatalogTourImageUploadRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return Task.FromResult<CatalogMediaImageDto?>(null);
    }

    public Task<IReadOnlyList<CatalogMediaImageDto>> GetTourImages(Guid id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return Task.FromResult<IReadOnlyList<CatalogMediaImageDto>>([]);
    }

    public Task<CatalogMediaImageDto?> ReviewMediaImageAccessibility(
        Guid id,
        PublicMediaImageAccessibilityReviewRequest request,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return Task.FromResult<CatalogMediaImageDto?>(null);
    }

    public Task<PublicMediaObjectResponse?> GetMediaPreview(Guid id, int width, string format, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return Task.FromResult<PublicMediaObjectResponse?>(null);
    }
}
