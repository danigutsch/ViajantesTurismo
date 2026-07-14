using System.Net.Http.Json;
using System.Text.Json;

namespace ViajantesTurismo.Admin.Testing.Integration.Helpers;

/// <summary>
/// Acquires local Keycloak conformance tokens for hosted API tests.
/// </summary>
public static class KeycloakConformanceClient
{
    private const string ClientId = "conformance-test-client";
    private const string Username = "conformance";

    /// <summary>
    /// Requests an access token for the supplied local API scopes.
    /// </summary>
    /// <param name="identityProviderEndpoint">The Keycloak HTTP endpoint.</param>
    /// <param name="password">The generated conformance-user password.</param>
    /// <param name="scopes">The API client scopes to request.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The issued access token.</returns>
    public static async Task<string> RequestAccessToken(
        Uri identityProviderEndpoint,
        string password,
        IReadOnlyCollection<string> scopes,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(identityProviderEndpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentNullException.ThrowIfNull(scopes);

        using var client = new HttpClient { BaseAddress = identityProviderEndpoint };
        using var request = new HttpRequestMessage(HttpMethod.Post, "/realms/viajantes/protocol/openid-connect/token")
        {
            Content = new FormUrlEncodedContent(
            [
                KeyValuePair.Create("grant_type", "password"),
                KeyValuePair.Create("client_id", ClientId),
                KeyValuePair.Create("username", Username),
                KeyValuePair.Create("password", password),
                KeyValuePair.Create("scope", string.Join(' ', scopes))
            ])
        };
        using var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Keycloak token request failed with status code {(int)response.StatusCode} ({response.StatusCode}).",
                null,
                response.StatusCode);
        }

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        return payload.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("Keycloak did not return an access token.");
    }
}
