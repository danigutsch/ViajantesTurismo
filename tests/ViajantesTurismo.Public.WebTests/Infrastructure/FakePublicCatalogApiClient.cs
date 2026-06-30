using System.Collections.Concurrent;
using System.Text.Json;

namespace ViajantesTurismo.Public.WebTests.Infrastructure;

internal sealed class FakePublicCatalogApiClient : IPublicCatalogApiClient
{
    private readonly List<CatalogTourDto> tours = [];
    private readonly ConcurrentDictionary<string, PublicContentVariantDto> contentByKeyAndCulture = new(StringComparer.OrdinalIgnoreCase);
    private PublicThemeSettingsDto theme = PublicThemeSettingsDefaults.CreateDto();

    public bool FailListRequests { get; set; }

    public bool FailDetailsRequests { get; set; }

    public bool FailContentRequests { get; set; }

    public bool FailThemeRequests { get; set; }

    public bool ReturnEmptyThemeResponse { get; set; }

    public bool ReturnMalformedThemeResponse { get; set; }

    public bool ReturnUnsupportedThemeResponse { get; set; }

    public TimeSpan ListDelay { get; set; }

    public TimeSpan ContentDelay { get; set; }

    public TaskCompletionSource<object?>? ListStarted { get; set; }

    public TaskCompletionSource<object?>? ContentStarted { get; set; }

    public void AddTour(CatalogTourDto tour)
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

    public void SetTheme(PublicThemeSettingsDto theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        this.theme = theme;
    }

    public async Task<CatalogTourDto[]> GetPublishedTours(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ListStarted?.TrySetResult(null);

        if (ListDelay > TimeSpan.Zero)
        {
            await Task.Delay(ListDelay, ct);
        }

        return FailListRequests
            ? throw new HttpRequestException("Catalog unavailable.")
            : tours.Where(tour => tour.IsPublished).ToArray();
    }

    public Task<CatalogTourDto?> GetPublishedTourBySlug(string slug, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (FailDetailsRequests)
        {
            throw new HttpRequestException("Catalog unavailable.");
        }

        var tour = tours.FirstOrDefault(tour =>
            tour.IsPublished && string.Equals(tour.Slug, slug, StringComparison.Ordinal));
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

        if (FailContentRequests)
        {
            throw new HttpRequestException("Catalog unavailable.");
        }

        var requestedCulture = string.IsNullOrWhiteSpace(culture) ? "en-US" : culture;
        contentByKeyAndCulture.TryGetValue(CreateContentKey(key, requestedCulture), out var content);
        return content;
    }

    public Task<PublicThemeSettingsDto> GetThemeSettings(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (FailThemeRequests)
        {
            throw new HttpRequestException("Catalog unavailable.");
        }

        if (ReturnEmptyThemeResponse)
        {
            throw new InvalidOperationException("Catalog returned an empty public theme response.");
        }

        ThrowIfThemeContentShouldFail();
        return Task.FromResult(theme);
    }

    private void ThrowIfThemeContentShouldFail()
    {
        if (ReturnMalformedThemeResponse)
        {
            throw new JsonException("Catalog returned malformed public theme JSON.");
        }

        if (ReturnUnsupportedThemeResponse)
        {
            throw new NotSupportedException("Catalog returned unsupported public theme content.");
        }
    }

    private static string CreateContentKey(string key, string culture) => $"{key}\u001F{culture}";
}
