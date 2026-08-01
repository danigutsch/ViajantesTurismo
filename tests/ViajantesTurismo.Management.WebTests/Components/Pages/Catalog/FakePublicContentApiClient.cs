using System.Net;
using SharedKernel.HttpClients;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Catalog;

internal sealed class FakePublicContentApiClient : IPublicContentApiClient
{
    public PublicContentDto[] Content { get; set; } = [];

    public bool ThrowOnGetContent { get; set; }

    public ContractValidationException? ValidationException { get; set; }

    public string? SavedKey { get; private set; }

    public UpsertPublicContentRequest? SavedRequest { get; private set; }

    public Task<ContractCommandOutcomeDto<PublicContentDto[]>> GetContent(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return Task.FromResult(ThrowOnGetContent
            ? new ContractCommandOutcomeDto<PublicContentDto[]>
            {
                Kind = ContractCommandOutcomeKind.TransportFailure,
                Message = "HTTP transport failed."
            }
            : ContractCommandOutcome.Succeeded(HttpStatusCode.OK, Content));
    }

    public Task<ContractCommandOutcomeDto<PublicContentDto>> GetContent(string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var content = Content.FirstOrDefault(entry => string.Equals(entry.Key, key, StringComparison.Ordinal));
        return Task.FromResult(content is null
            ? ContractCommandOutcome.Status<PublicContentDto>(ContractCommandOutcomeKind.NotFound, HttpStatusCode.NotFound)
            : ContractCommandOutcome.Succeeded(HttpStatusCode.OK, content));
    }

    public Task<ContractCommandOutcomeDto<PublicContentDto>> SaveContent(string key, UpsertPublicContentRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (ValidationException is not null)
        {
            return Task.FromResult(new ContractCommandOutcomeDto<PublicContentDto>
            {
                Kind = ContractCommandOutcomeKind.ValidationProblem,
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

        return Task.FromResult(ContractCommandOutcome.Succeeded(HttpStatusCode.OK, saved));
    }
}
