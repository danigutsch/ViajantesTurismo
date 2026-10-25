using System.Net;
using System.Net.Http.Json;
using PandaBear.Branding;
using PandaBear.HttpClients;

namespace ViajantesTurismo.Branding.Contracts.Http;

/// <summary>HTTP client for the Branding management settings endpoint.</summary>
/// <param name="httpClient">The configured HTTP client.</param>
public sealed class BrandingManagementClient(HttpClient httpClient) : IBrandingManagementClient
{
    internal const int MaximumSettingsResponseBytes = 1024 * 1024;
    private const string ManagementSettingsRequestPath = $"/api/v1/{BrandingRoutes.ManagementSettingsPath}";
    private static readonly BrandingManagementJsonContext Json = BrandingManagementJsonContext.Default;

    /// <inheritdoc />
    public async Task<BrandingManagementDocument> GetSettings(CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(ManagementSettingsRequestPath, UriKind.Relative));
        return await Send(request, static async (response, token) =>
        {
            response.EnsureSuccessStatusCode();
            var entityTag = response.Headers.ETag is { IsWeak: false } tag
                ? tag.Tag
                : throw new InvalidOperationException("Branding API returned settings without a strong entity tag.");
            return new BrandingManagementDocument(await ReadSettings(response, token).ConfigureAwait(false), entityTag);
        }, ct).ConfigureAwait(false);
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
        return await Send(httpRequest, static async (response, token) =>
        {
            if (response.StatusCode == HttpStatusCode.PreconditionFailed)
            {
                throw new BrandingPreconditionFailedException();
            }

            await ContractHttpValidation.EnsureSuccessOrThrowValidationException(
                response,
                Json.ContractValidationProblemDto,
                token).ConfigureAwait(false);
            return await ReadSettings(response, token).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);
    }

    // The body is read after the headers, so the client timeout and the response-size ceiling are applied here.
    private async Task<T> Send<T>(
        HttpRequestMessage request,
        Func<HttpResponseMessage, CancellationToken, Task<T>> read,
        CancellationToken ct)
    {
        using var timeoutSource = httpClient.Timeout == Timeout.InfiniteTimeSpan
            ? null
            : CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutSource?.CancelAfter(httpClient.Timeout);
        var requestToken = timeoutSource?.Token ?? ct;
        try
        {
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, requestToken)
                .ConfigureAwait(false);
            var responseLimit = Math.Min(httpClient.MaxResponseContentBufferSize, MaximumSettingsResponseBytes);
            if (response.Content.Headers.ContentLength > responseLimit)
            {
                throw new HttpRequestException("Branding API response body exceeds the maximum response bytes.");
            }

            await response.Content.LoadIntoBufferAsync(responseLimit, requestToken).ConfigureAwait(false);
            return await read(response, requestToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception) when (ct.IsCancellationRequested)
        {
            throw new OperationCanceledException(exception.Message, exception, ct);
        }
    }

    private static async Task<BrandingManagementSettingsDto> ReadSettings(HttpResponseMessage response, CancellationToken ct) =>
        await response.Content.ReadFromJsonAsync(Json.BrandingManagementSettingsDto, ct).ConfigureAwait(false)
        ?? throw new InvalidOperationException("Branding API returned an empty settings response.");
}
