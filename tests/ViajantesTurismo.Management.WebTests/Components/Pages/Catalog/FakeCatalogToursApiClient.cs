using System.Net;
using SharedKernel.HttpClients;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Catalog;

internal sealed class FakeCatalogToursApiClient : ICatalogToursApiClient
{
    public CatalogTourDto[] Tours { get; set; } = [];

    public bool ThrowOnGetTours { get; set; }

    public ContractValidationException? ValidationException { get; set; }

    public bool ThrowConflictOnUpdate { get; set; }

    public bool ThrowAcceptedOnUpdate { get; set; }

    public bool ThrowAcceptedOnPublication { get; set; }

    public UpsertCatalogTourPresentationRequest? LastPresentationRequest { get; private set; }

    public CatalogTourPublicationRequest? LastPublicationRequest { get; private set; }

    public bool? LastPublicationState { get; private set; }

    public IReadOnlyList<CatalogMediaImageDto> Images { get; set; } = [];

    public bool ThrowOnGetTourImages { get; set; }

    public CatalogMediaImageDto? Draft { get; set; }

    public CatalogMediaImageDto? UploadedImage { get; set; }

    public Guid? LastUploadedTourId { get; private set; }

    public string? LastUploadedFileName { get; private set; }

    public string? LastUploadedContentType { get; private set; }

    public string? LastUploadedAltText { get; private set; }

    public string? LastUploadedCaption { get; private set; }

    public CatalogMediaImageDto? AccessibilityReviewResult { get; set; }

    public bool ReturnNullOnAccessibilityReview { get; set; }

    public bool ThrowOnSubsequentGetTourImages { get; set; }

    public PublicMediaObjectResponse? Media { get; set; }

    public bool ThrowOnMediaPreview { get; set; }

    public Guid? LastMediaId { get; private set; }

    public int? LastMediaWidth { get; private set; }

    public string? LastMediaFormat { get; private set; }

    public PublicMediaImageAccessibilityReviewRequest? LastAccessibilityReviewRequest { get; private set; }

    private int getTourImagesRequests;

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

        LastPresentationRequest = request;
        if (ThrowAcceptedOnUpdate)
        {
            throw new HttpRequestException("Catalog tour projection pending.", null, HttpStatusCode.Accepted);
        }

        if (ThrowConflictOnUpdate)
        {
            throw new HttpRequestException("Catalog tour version conflict.", null, HttpStatusCode.Conflict);
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
            Summary = request.Summary,
            Description = request.Description,
            Itinerary = request.Itinerary,
            SeoTitle = request.SeoTitle,
            SeoDescription = request.SeoDescription,
            Version = tour.Version + 1
        };

        Tours = Tours.Select(current => current.Id == id ? updated : current).ToArray();
        return Task.FromResult<CatalogTourDto?>(updated);
    }

    public Task Publish(Guid id, CatalogTourPublicationRequest request, CancellationToken ct)
    {
        return ChangePublication(id, request, isPublished: true, ct);
    }

    public Task Unpublish(Guid id, CatalogTourPublicationRequest request, CancellationToken ct)
    {
        return ChangePublication(id, request, isPublished: false, ct);
    }

    public Task<CatalogMediaImageDto?> GenerateMediaImageAccessibilityDraft(Guid id, PublicMediaImageAccessibilityDraftRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return Task.FromResult(Draft);
    }

    public Task<CatalogMediaImageDto?> UploadTourImage(Guid id, CatalogTourImageUploadRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        LastUploadedTourId = id;
        LastUploadedFileName = request.FileName;
        LastUploadedContentType = request.ContentType;
        LastUploadedAltText = request.AltText;
        LastUploadedCaption = request.Caption;
        return Task.FromResult(UploadedImage);
    }

    public Task<IReadOnlyList<CatalogMediaImageDto>> GetTourImages(Guid id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (ThrowOnGetTourImages)
        {
            throw new HttpRequestException("Catalog unavailable.");
        }

        if (ThrowOnSubsequentGetTourImages && Interlocked.Increment(ref getTourImagesRequests) > 1)
        {
            throw new HttpRequestException("Catalog unavailable.");
        }

        return Task.FromResult(Images);
    }

    public Task<CatalogMediaImageDto?> ReviewMediaImageAccessibility(Guid id, PublicMediaImageAccessibilityReviewRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (ValidationException is not null)
        {
            throw ValidationException;
        }

        LastAccessibilityReviewRequest = request;
        if (ReturnNullOnAccessibilityReview)
        {
            return Task.FromResult<CatalogMediaImageDto?>(null);
        }

        var reviewed = AccessibilityReviewResult ?? Images.FirstOrDefault(image => image.Id == id);
        if (reviewed is not null)
        {
            Images = Images.Select(image => image.Id == id ? reviewed : image).ToArray();
        }

        return Task.FromResult(reviewed);
    }

    public Task<PublicMediaObjectResponse?> GetMediaPreview(Guid id, int width, string format, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        LastMediaId = id;
        LastMediaWidth = width;
        LastMediaFormat = format;

        if (ThrowOnMediaPreview)
        {
            throw new HttpRequestException("Catalog unavailable.");
        }

        return Task.FromResult(Media);
    }

    private Task ChangePublication(
        Guid id,
        CatalogTourPublicationRequest request,
        bool isPublished,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        LastPublicationRequest = request;
        LastPublicationState = isPublished;

        if (ThrowAcceptedOnPublication)
        {
            throw new HttpRequestException("Catalog tour projection pending.", null, HttpStatusCode.Accepted);
        }

        var tour = Tours.SingleOrDefault(tour => tour.Id == id);
        if (tour is not null)
        {
            var updated = tour with { IsPublished = isPublished, Version = tour.Version + 1 };
            Tours = Tours.Select(current => current.Id == id ? updated : current).ToArray();
        }

        return Task.CompletedTask;
    }
}
