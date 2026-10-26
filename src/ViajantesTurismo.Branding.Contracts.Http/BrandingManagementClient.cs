using System.Net;
using System.Net.Http.Json;
using PandaBear.Branding;
using PandaBear.HttpClients;

namespace ViajantesTurismo.Branding.Contracts.Http;

/// <summary>HTTP client for the Branding management settings endpoint.</summary>
/// <param name="httpClient">The configured HTTP client.</param>
public sealed class BrandingManagementClient(HttpClient httpClient) : IBrandingManagementClient
{
    private const string ManagementSettingsRequestPath = $"/api/v1/{BrandingRoutes.ManagementSettingsPath}";
    private const string MissingStrongEntityTagMessage = "Branding API returned settings without a strong entity tag.";
    private static readonly BrandingManagementJsonContext Json = BrandingManagementJsonContext.Default;

    /// <inheritdoc />
    public async Task<HttpOutcome<BrandingManagementDocument>> GetSettings(CancellationToken ct)
    {
        var outcome = await HttpOutcomeMapper.Send(
            httpClient,
            new HttpRequestMessage(HttpMethod.Get, new Uri(ManagementSettingsRequestPath, UriKind.Relative)),
            HttpStatusCode.OK,
            Json.BrandingManagementSettingsDto,
            Json.ContractValidationProblemDto,
            ct).ConfigureAwait(false);

        if (outcome is not { Kind: HttpOutcomeKind.Succeeded, Value: { } settings, StatusCode: { } status })
        {
            return new HttpOutcome<BrandingManagementDocument>
            {
                Kind = outcome.Kind,
                StatusCode = outcome.StatusCode,
                ValidationErrors = outcome.ValidationErrors,
                Message = outcome.Message
            };
        }

        // The validator is opaque to the client; only a weak or missing tag cannot guard the next full-state PUT.
        return outcome.ETag is { } entityTag && !entityTag.StartsWith("W/", StringComparison.Ordinal)
            ? HttpOutcomeMapper.Succeeded(status, new BrandingManagementDocument(settings, entityTag))
            : HttpOutcomeMapper.Status<BrandingManagementDocument>(HttpOutcomeKind.MalformedBody, status, MissingStrongEntityTagMessage);
    }

    /// <inheritdoc />
    public async Task<HttpOutcome<BrandingManagementSettingsDto>> SaveSettings(
        BrandingSettingsDto request,
        string entityTag,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityTag);

        var httpRequest = new HttpRequestMessage(HttpMethod.Put, new Uri(ManagementSettingsRequestPath, UriKind.Relative))
        {
            Content = JsonContent.Create(request, Json.BrandingSettingsDto)
        };
        httpRequest.Headers.IfMatch.ParseAdd(entityTag);
        return await HttpOutcomeMapper.Send(
            httpClient,
            httpRequest,
            HttpStatusCode.OK,
            Json.BrandingManagementSettingsDto,
            Json.ContractValidationProblemDto,
            ct).ConfigureAwait(false);
    }
}
