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

    /// <summary>Gets the local Keycloak username for the Operator conformance user.</summary>
    public const string OperatorUsername = "operator-conformance";

    /// <summary>Gets the opaque Keycloak identifier for the local conformance user.</summary>
    public const string ConformanceUserId = "9f0e2348-6f2d-4d67-a6e4-18bf9d4b7f23";

    /// <summary>
    /// Requests an access token for the supplied local API scopes.
    /// </summary>
    /// <param name="identityProviderEndpoint">The Keycloak HTTP endpoint.</param>
    /// <param name="password">The generated conformance-user password.</param>
    /// <param name="scopes">The API client scopes to request.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <param name="username">The local Keycloak username.</param>
    /// <returns>The issued access token.</returns>
    public static async Task<string> RequestAccessToken(
        Uri identityProviderEndpoint,
        string password,
        IReadOnlyCollection<string> scopes,
        CancellationToken ct,
        string username = Username)
    {
        ArgumentNullException.ThrowIfNull(identityProviderEndpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentNullException.ThrowIfNull(scopes);

        using var client = new HttpClient { BaseAddress = identityProviderEndpoint };
        using var request = new HttpRequestMessage(HttpMethod.Post, "/realms/viajantes/protocol/openid-connect/token")
        {
            Content = new FormUrlEncodedContent(
            [
                KeyValuePair.Create("grant_type", "password"),
                KeyValuePair.Create("client_id", ClientId),
                KeyValuePair.Create("username", username),
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
