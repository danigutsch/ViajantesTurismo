using ViajantesTurismo.Management.Web;
using ViajantesTurismo.Management.Web.Exceptions;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Catalog;

internal sealed class FakePublicContentApiClient : IPublicContentApiClient
{
    public PublicContentDto[] Content { get; set; } = [];

    public bool ThrowOnGetContent { get; set; }

    public ApiValidationException? ValidationException { get; set; }

    public string? SavedKey { get; private set; }

    public UpsertPublicContentRequest? SavedRequest { get; private set; }

    public Task<PublicContentDto[]> GetContent(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return ThrowOnGetContent
            ? throw new HttpRequestException("Public content unavailable.")
            : Task.FromResult(Content);
    }

    public Task<PublicContentDto?> GetContent(string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return Task.FromResult(Content.FirstOrDefault(entry => string.Equals(entry.Key, key, StringComparison.Ordinal)));
    }

    public Task<PublicContentDto> SaveContent(string key, UpsertPublicContentRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (ValidationException is not null)
        {
            throw ValidationException;
        }

        SavedKey = key;
        SavedRequest = request;

        var saved = new PublicContentDto
        {
            Key = key,
            SourceLanguage = request.SourceLanguage,
            PublicationState = request.Variants.Any(variant => variant.RequiresHumanReview)
                ? "ReviewRequired"
                : "Draft"
        };

        foreach (var variant in request.Variants)
        {
            saved.Variants.Add(variant);
        }

        Content = [saved];

        return Task.FromResult(saved);
    }
}
