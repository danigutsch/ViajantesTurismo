using System.Net.Http.Json;
using SharedKernel.HttpClients;
using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.Contracts.Http;

/// <summary>
/// HTTP client for public theme management endpoints.
/// </summary>
public sealed class PublicThemeApiClient(HttpClient httpClient) : IPublicThemeApiClient
{
    private const string RoutePrefix = "/api/v1/catalog/public-theme";
    private static readonly PublicThemeApiClientJsonContext Json = PublicThemeApiClientJsonContext.Default;

    /// <inheritdoc />
    public async Task<PublicThemeSettingsDto> GetTheme(CancellationToken ct)
    {
        var theme = await httpClient.GetFromJsonAsync(RoutePrefix, Json.PublicThemeSettingsDto, ct).ConfigureAwait(false);
        return theme ?? throw new InvalidOperationException("Catalog API returned an empty theme response.");
    }

    /// <inheritdoc />
    public async Task<PublicThemeSettingsDto> SaveTheme(PublicThemeSettingsDto request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var response = await httpClient.PutAsJsonAsync(RoutePrefix, request, Json.PublicThemeSettingsDto, ct).ConfigureAwait(false);
        await ContractHttpValidation.EnsureSuccessOrThrowValidationException(response, Json.ContractValidationProblemDto, ct).ConfigureAwait(false);

        var theme = await response.Content.ReadFromJsonAsync(Json.PublicThemeSettingsDto, ct).ConfigureAwait(false);
        return theme ?? throw new InvalidOperationException("Catalog API returned an empty theme response.");
    }
}
