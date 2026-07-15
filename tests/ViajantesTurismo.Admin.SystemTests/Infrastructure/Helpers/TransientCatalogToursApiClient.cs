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

    public Task<PublicMediaImageDto?> GenerateMediaImageAccessibilityDraft(
        Guid id,
        PublicMediaImageAccessibilityDraftRequest request,
        CancellationToken ct)
    {
        return Task.FromResult<PublicMediaImageDto?>(null);
    }
}
