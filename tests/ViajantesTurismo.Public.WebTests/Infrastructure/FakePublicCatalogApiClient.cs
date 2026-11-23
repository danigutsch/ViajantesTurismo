using PandaBear.BuildingBlocks;
using System.Collections.Concurrent;
using PandaBear.HttpClients;

namespace ViajantesTurismo.Public.WebTests.Infrastructure;

/// <summary>In-memory <see cref="IPublicCatalogApiClient"/> fake with failure, cancellation and request controls.</summary>
internal sealed class FakePublicCatalogApiClient : IPublicCatalogApiClient
{
    private readonly List<TourSummaryDto> tours = [];
    private readonly List<TourDetailsDto> tourDetails = [];
    private readonly ConcurrentDictionary<string, PublicContentVariantDto> contentByKeyAndCulture = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>When true, the tour list returns a transport failure.</summary>
    public bool FailListRequests
    {
        get; set;
    }

    /// <summary>When true, the tour list throws <see cref="OperationCanceledException"/>.</summary>
    public bool ThrowOperationCanceledExceptionOnListRequests
    {
        get; set;
    }

    /// <summary>When true, media requests throw <see cref="OperationCanceledException"/>.</summary>
    public bool ThrowOperationCanceledExceptionOnMediaRequests
    {
        get; set;
    }

    /// <summary>When true, media requests throw <see cref="HttpRequestException"/>.</summary>
    public bool FailMediaRequests
    {
        get; set;
    }

    /// <summary>When true, tour detail requests return a transport failure.</summary>
    public bool FailDetailsRequests
    {
        get; set;
    }

    /// <summary>When true, tour detail requests throw <see cref="OperationCanceledException"/>.</summary>
    public bool ThrowOperationCanceledExceptionOnDetailsRequests
    {
        get; set;
    }

    /// <summary>When true, content requests return a transport failure.</summary>
    public bool FailContentRequests
    {
        get; set;
    }

    /// <summary>When true, content requests throw <see cref="OperationCanceledException"/>.</summary>
    public bool ThrowOperationCanceledExceptionOnContentRequests
    {
        get; set;
    }

    /// <summary>Optional task that holds tour list and content responses until the test releases them.</summary>
    public Task? ListAndContentResponseGate
    {
        get; set;
    }

    /// <summary>Completed when a tour list request starts, if set.</summary>
    public TaskCompletionSource? ListStarted
    {
        get; set;
    }

    /// <summary>Completed when a content request starts, if set.</summary>
    public TaskCompletionSource? ContentStarted
    {
        get; set;
    }

    /// <summary>Response returned by media requests; null simulates a missing object.</summary>
    public PublicMediaObjectResponse? Media
    {
        get; set;
    }

    /// <summary>Id of the latest media request.</summary>
    public Guid? LastMediaId
    {
        get; private set;
    }

    /// <summary>Width of the latest media request.</summary>
    public int? LastMediaWidth
    {
        get; private set;
    }

    /// <summary>Format of the latest media request.</summary>
    public string? LastMediaFormat
    {
        get; private set;
    }

    /// <summary>Adds a tour to both the details store and the summary list.</summary>
    public void AddTour(TourDetailsDto tour)
    {
        ArgumentNullException.ThrowIfNull(tour);

        tourDetails.Add(tour);
        tours.Add(new TourSummaryDto
        {
            Title = tour.Title,
            Slug = tour.Slug,
            Summary = tour.Summary,
            Images = tour.Images,
            UpdatedAt = tour.UpdatedAt
        });
    }

    /// <summary>Adds a tour to the summary list only.</summary>
    public void AddTour(TourSummaryDto tour)
    {
        ArgumentNullException.ThrowIfNull(tour);

        tours.Add(tour);
    }

    /// <summary>Adds content for <c>home.hero</c> in the given culture.</summary>
    public void AddContent(string culture, PublicContentVariantDto content)
    {
        AddContent("home.hero", culture, content);
    }

    /// <summary>Adds content for the given key and culture; keys compare case-insensitively.</summary>
    public void AddContent(string key, string culture, PublicContentVariantDto content)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(culture);

        contentByKeyAndCulture[CreateContentKey(key, culture)] = content;
    }

    /// <summary>Waits for the startup signals and releases the responses even when the wait fails.</summary>
    public static async Task ReleaseResponsesAfter(Task requestsStarted, TaskCompletionSource allowResponses, CancellationToken ct)
    {
        try
        {
            await requestsStarted.WaitAsync(ct);
        }
        finally
        {
            allowResponses.TrySetResult();
        }
    }

    /// <summary>Returns the tour summaries after any configured response gate.</summary>
    public async Task<HttpOutcome<TourSummaryDto[]>> GetPublishedTours(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ListStarted?.TrySetResult();

        if (ListAndContentResponseGate is { } responseGate)
        {
            await responseGate.WaitAsync(ct);
        }

        if (ThrowOperationCanceledExceptionOnListRequests)
        {
            throw new OperationCanceledException("Catalog request canceled upstream.");
        }

        return FailListRequests
            ? TransportFailure<TourSummaryDto[]>()
            : HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, tours.ToArray());
    }

    /// <summary>Returns the tour with the given slug, not found if absent, honoring the failure switches.</summary>
    public Task<HttpOutcome<TourDetailsDto>> GetPublishedTourBySlug(string slug, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (ThrowOperationCanceledExceptionOnDetailsRequests)
        {
            throw new OperationCanceledException("Catalog details request canceled upstream.");
        }

        if (FailDetailsRequests)
        {
            return Task.FromResult(TransportFailure<TourDetailsDto>());
        }

        var tour = tourDetails.FirstOrDefault(tour => string.Equals(tour.Slug, slug, StringComparison.Ordinal));
        return Task.FromResult(tour is null
            ? HttpOutcomeMapper.Status<TourDetailsDto>(HttpOutcomeKind.NotFound, HttpStatusCode.NotFound)
            : HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, tour));
    }

    /// <summary>Returns content for the key and culture (<c>en-US</c> when blank), not found if absent, honoring the failure switches.</summary>
    public async Task<HttpOutcome<PublicContentVariantDto>> GetPublicContent(string key, string? culture, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ContentStarted?.TrySetResult();

        if (ListAndContentResponseGate is { } responseGate)
        {
            await responseGate.WaitAsync(ct);
        }

        if (ThrowOperationCanceledExceptionOnContentRequests)
        {
            throw new OperationCanceledException("Catalog content request canceled upstream.");
        }

        if (FailContentRequests)
        {
            return TransportFailure<PublicContentVariantDto>();
        }

        var requestedCulture = string.IsNullOrWhiteSpace(culture) ? "en-US" : culture;
        contentByKeyAndCulture.TryGetValue(CreateContentKey(key, requestedCulture), out var content);
        return content is null
            ? HttpOutcomeMapper.Status<PublicContentVariantDto>(HttpOutcomeKind.NotFound, HttpStatusCode.NotFound)
            : HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, content);
    }

    /// <summary>Records the requested id, width and format, then returns <see cref="Media"/> or throws per the failure switches.</summary>
    public Task<PublicMediaObjectResponse?> GetPublicMedia(Guid id, int width, string format, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        LastMediaId = id;
        LastMediaWidth = width;
        LastMediaFormat = format;

        if (ThrowOperationCanceledExceptionOnMediaRequests)
        {
            throw new OperationCanceledException("Catalog media request canceled upstream.");
        }

        if (FailMediaRequests)
        {
            throw new HttpRequestException("Catalog unavailable.");
        }

        return Task.FromResult(Media);
    }

    /// <inheritdoc />
    public async Task<HttpOutcome<CatalogGalleryIndexDto>> ListPublishedAlbums(string culture, string? page, CancellationToken ct)
    {
        var published = await GetPublishedTours(ct);
        if (published.Value is not { } summaries)
        {
            return TransportFailure<CatalogGalleryIndexDto>();
        }

        var albums = summaries.Where(tour => tour.Images.Count > 0).Select(tour => new CatalogGallerySummaryDto(
            tour.Slug, tour.Title, "en-US", tour.Images.Count,
            tour.Images.OrderByDescending(image => image.IsCover).ThenBy(image => image.SortOrder).First())).ToArray();
        var window = PageWindow.Create(page, 24, albums.Length);

        return window.IsOutOfRange
            ? HttpOutcomeMapper.Status<CatalogGalleryIndexDto>(HttpOutcomeKind.NotFound, HttpStatusCode.NotFound)
            : HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, new CatalogGalleryIndexDto(
                window.Page, window.PageSize, window.Total, [.. albums.Skip(window.Skip).Take(window.Take)]));
    }

    /// <inheritdoc />
    public Task<HttpOutcome<CatalogGalleryAlbumDto>> GetPublishedAlbum(string slug, string culture, string? page, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (ThrowOperationCanceledExceptionOnDetailsRequests)
        {
            throw new OperationCanceledException("Catalog album request canceled upstream.");
        }

        if (FailDetailsRequests)
        {
            return Task.FromResult(TransportFailure<CatalogGalleryAlbumDto>());
        }

        var tour = tours.Find(tour => string.Equals(tour.Slug, slug, StringComparison.Ordinal));
        var window = PageWindow.Create(page, 24, tour?.Images.Count ?? 0);
        if (tour is null || window.Total == 0 || window.IsOutOfRange)
        {
            return Task.FromResult(HttpOutcomeMapper.Status<CatalogGalleryAlbumDto>(HttpOutcomeKind.NotFound, HttpStatusCode.NotFound));
        }

        return Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, new CatalogGalleryAlbumDto(
            tour.Slug, tour.Title, "en-US", window.Page, window.PageSize, window.Total,
            [.. tour.Images.OrderBy(image => image.SortOrder).Skip(window.Skip).Take(window.Take)])));
    }

    private static string CreateContentKey(string key, string culture) => $"{key}\u001F{culture}";

    private static HttpOutcome<TValue> TransportFailure<TValue>() => new()
    {
        Kind = HttpOutcomeKind.TransportFailure,
        Message = "HTTP transport failed."
    };
}
