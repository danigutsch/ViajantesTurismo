using SharedKernel.HttpClients;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Catalog;

internal sealed class FakeCatalogToursApiClient : ICatalogToursApiClient
{
    public CatalogTourDto[] Tours { get; set; } = [];

    public bool ThrowOnGetTours { get; set; }

    public ContractValidationException? ValidationException { get; set; }

    public IReadOnlyList<CatalogMediaImageDto> Images { get; set; } = [];

    public CatalogMediaImageDto? Draft { get; set; }

    public PublicMediaObjectResponse? Media { get; set; }

    public Guid? LastMediaId { get; private set; }

    public int? LastMediaWidth { get; private set; }

    public string? LastMediaFormat { get; private set; }

    public PublicMediaImageAccessibilityReviewRequest? LastAccessibilityReviewRequest { get; private set; }

    public Task<CatalogTourDto[]> GetTours(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return ThrowOnGetTours
            ? throw new HttpRequestException("Catalog unavailable.")
            : Task.FromResult(Tours);
    }

    public Task<CatalogTourDto?> GetTour(Guid id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return ThrowOnGetTours
            ? throw new HttpRequestException("Catalog unavailable.")
            : Task.FromResult(Tours.SingleOrDefault(tour => tour.Id == id));
    }

    public Task<CatalogTourDto?> UpdatePresentation(Guid id, UpsertCatalogTourPresentationRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (ValidationException is not null)
        {
            throw ValidationException;
        }

        var tour = Tours.SingleOrDefault(tour => tour.Id == id);
        if (tour is null)
        {
            return Task.FromResult<CatalogTourDto?>(null);
        }

        var updated = tour with
        {
            Title = request.Title,
            Slug = request.Slug,
            IsPublished = request.IsPublished
        };

        Tours = Tours.Select(current => current.Id == id ? updated : current).ToArray();
        return Task.FromResult<CatalogTourDto?>(updated);
    }

    public Task<CatalogMediaImageDto?> GenerateMediaImageAccessibilityDraft(Guid id, PublicMediaImageAccessibilityDraftRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return Task.FromResult(Draft);
    }

    public Task<CatalogMediaImageDto?> UploadTourImage(Guid id, CatalogTourImageUploadRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return Task.FromResult<CatalogMediaImageDto?>(null);
    }

    public Task<IReadOnlyList<CatalogMediaImageDto>> GetTourImages(Guid id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return Task.FromResult(Images);
    }

    public Task<CatalogMediaImageDto?> ReviewMediaImageAccessibility(Guid id, PublicMediaImageAccessibilityReviewRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        LastAccessibilityReviewRequest = request;
        return Task.FromResult(Images.FirstOrDefault(image => image.Id == id));
    }

    public Task<PublicMediaObjectResponse?> GetMediaPreview(Guid id, int width, string format, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        LastMediaId = id;
        LastMediaWidth = width;
        LastMediaFormat = format;

        return Task.FromResult(Media);
    }
}
