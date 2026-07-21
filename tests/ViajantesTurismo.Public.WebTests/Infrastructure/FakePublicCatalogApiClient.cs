using System.Collections.Concurrent;

namespace ViajantesTurismo.Public.WebTests.Infrastructure;

internal sealed class FakePublicCatalogApiClient : IPublicCatalogApiClient
{
    private readonly List<TourSummaryDto> tours = [];
    private readonly List<TourDetailsDto> tourDetails = [];
    private readonly ConcurrentDictionary<string, PublicContentVariantDto> contentByKeyAndCulture = new(StringComparer.OrdinalIgnoreCase);

    public bool FailListRequests { get; set; }

    public bool ThrowOperationCanceledExceptionOnListRequests { get; set; }

    public bool ThrowOperationCanceledExceptionOnMediaRequests { get; set; }

    public bool FailMediaRequests { get; set; }

    public bool FailDetailsRequests { get; set; }

    public bool ThrowOperationCanceledExceptionOnDetailsRequests { get; set; }

    public bool FailContentRequests { get; set; }

    public bool ThrowOperationCanceledExceptionOnContentRequests { get; set; }

    public TimeSpan ListDelay { get; set; }

    public TimeSpan ContentDelay { get; set; }

    public TaskCompletionSource<object?>? ListStarted { get; set; }

    public TaskCompletionSource<object?>? ContentStarted { get; set; }

    public PublicMediaObjectResponse? Media { get; set; }

    public Guid? LastMediaId { get; private set; }

    public int? LastMediaWidth { get; private set; }

    public string? LastMediaFormat { get; private set; }

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

    public void AddTour(TourSummaryDto tour)
    {
        ArgumentNullException.ThrowIfNull(tour);

        tours.Add(tour);
    }

    public void AddContent(string culture, PublicContentVariantDto content)
    {
        AddContent("home.hero", culture, content);
    }

    public void AddContent(string key, string culture, PublicContentVariantDto content)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(culture);

        contentByKeyAndCulture[CreateContentKey(key, culture)] = content;
    }

    public async Task<TourSummaryDto[]> GetPublishedTours(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ListStarted?.TrySetResult(null);

        if (ListDelay > TimeSpan.Zero)
        {
            await Task.Delay(ListDelay, ct);
        }

        if (ThrowOperationCanceledExceptionOnListRequests)
        {
            throw new OperationCanceledException("Catalog request canceled upstream.");
        }

        return FailListRequests
            ? throw new HttpRequestException("Catalog unavailable.")
            : tours.ToArray();
    }

    public Task<TourDetailsDto?> GetPublishedTourBySlug(string slug, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (ThrowOperationCanceledExceptionOnDetailsRequests)
        {
            throw new OperationCanceledException("Catalog details request canceled upstream.");
        }

        if (FailDetailsRequests)
        {
            throw new HttpRequestException("Catalog unavailable.");
        }

        var tour = tourDetails.FirstOrDefault(tour => string.Equals(tour.Slug, slug, StringComparison.Ordinal));
        return Task.FromResult(tour);
    }

    public async Task<PublicContentVariantDto?> GetPublicContent(string key, string? culture, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ContentStarted?.TrySetResult(null);

        if (ContentDelay > TimeSpan.Zero)
        {
            await Task.Delay(ContentDelay, ct);
        }

        if (ThrowOperationCanceledExceptionOnContentRequests)
        {
            throw new OperationCanceledException("Catalog content request canceled upstream.");
        }

        if (FailContentRequests)
        {
            throw new HttpRequestException("Catalog unavailable.");
        }

        var requestedCulture = string.IsNullOrWhiteSpace(culture) ? "en-US" : culture;
        contentByKeyAndCulture.TryGetValue(CreateContentKey(key, requestedCulture), out var content);
        return content;
    }

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

    private static string CreateContentKey(string key, string culture) => $"{key}\u001F{culture}";
}
