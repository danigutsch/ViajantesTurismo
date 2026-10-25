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
    private static readonly BrandingManagementJsonContext Json = BrandingManagementJsonContext.Default;

    /// <inheritdoc />
    public async Task<BrandingManagementDocument> GetSettings(CancellationToken ct)
    {
        using var response = await httpClient.GetAsync(
            new Uri(ManagementSettingsRequestPath, UriKind.Relative),
            ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var entityTag = response.Headers.ETag is { IsWeak: false } tag
            ? tag.Tag
            : throw new InvalidOperationException("Branding API returned settings without a strong entity tag.");
        var settings = await response.Content.ReadFromJsonAsync(Json.BrandingManagementSettingsDto, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Branding API returned an empty settings response.");
        return new BrandingManagementDocument(settings, entityTag);
    }

    /// <inheritdoc />
    public async Task<BrandingManagementSettingsDto> SaveSettings(
        BrandingSettingsDto request,
        string entityTag,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityTag);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Put, new Uri(ManagementSettingsRequestPath, UriKind.Relative))
        {
            Content = JsonContent.Create(request, Json.BrandingSettingsDto)
        };
        httpRequest.Headers.IfMatch.ParseAdd(entityTag);
        using var response = await httpClient.SendAsync(httpRequest, ct).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.PreconditionFailed)
        {
            throw new BrandingPreconditionFailedException();
        }

        await ContractHttpValidation.EnsureSuccessOrThrowValidationException(
            response,
            Json.ContractValidationProblemDto,
            ct).ConfigureAwait(false);
        return await response.Content.ReadFromJsonAsync(Json.BrandingManagementSettingsDto, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Branding API returned an empty settings response.");
    }
}
