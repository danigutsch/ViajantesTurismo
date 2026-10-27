using System.Net;
using PandaBear.HttpClients;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Catalog;

/// <summary>In-memory public content API client that records the last saved key and request.</summary>
internal sealed class FakePublicContentApiClient : IPublicContentApiClient
{
    /// <summary>Content entries returned by the fake and replaced by a successful save.</summary>
    public PublicContentDto[] Content { get; set; } = [];

    /// <summary>When true, the list call returns a transport failure.</summary>
    public bool ThrowOnGetContent
    {
        get; set;
    }

    /// <summary>When set, a save returns a validation-problem outcome carrying its errors.</summary>
    public ContractValidationException? ValidationException
    {
        get; set;
    }

    /// <summary>Key passed to the last successful save, or null if none.</summary>
    public string? SavedKey
    {
        get; private set;
    }

    /// <summary>Request passed to the last successful save, or null if none.</summary>
    public UpsertPublicContentRequest? SavedRequest
    {
        get; private set;
    }

    /// <inheritdoc />
    public Task<HttpOutcome<PublicContentDto[]>> GetContent(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return Task.FromResult(ThrowOnGetContent
            ? new HttpOutcome<PublicContentDto[]>
            {
                Kind = HttpOutcomeKind.TransportFailure,
                Message = "HTTP transport failed."
            }
            : HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, Content));
    }

    /// <inheritdoc />
    public Task<HttpOutcome<PublicContentDto>> GetContent(string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var content = Content.FirstOrDefault(entry => string.Equals(entry.Key, key, StringComparison.Ordinal));
        return Task.FromResult(content is null
            ? HttpOutcomeMapper.Status<PublicContentDto>(HttpOutcomeKind.NotFound, HttpStatusCode.NotFound)
            : HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, content));
    }

    /// <inheritdoc />
    public Task<HttpOutcome<PublicContentDto>> SaveContent(string key, UpsertPublicContentRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (ValidationException is not null)
        {
            return Task.FromResult(new HttpOutcome<PublicContentDto>
            {
                Kind = HttpOutcomeKind.ValidationProblem,
                StatusCode = HttpStatusCode.BadRequest,
                ValidationErrors = ValidationException.ValidationErrors
            });
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

        return Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, saved));
    }
}
