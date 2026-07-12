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
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(30);

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

        using var startupTimeout = new CancellationTokenSource(StartupTimeout);
        using var startupCancellation = CancellationTokenSource.CreateLinkedTokenSource(ct, startupTimeout.Token);
        using var client = new HttpClient { BaseAddress = identityProviderEndpoint };

        while (true)
        {
            try
            {
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

                using var response = await client.SendAsync(request, startupCancellation.Token);
                response.EnsureSuccessStatusCode();
                var payload = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: startupCancellation.Token);
                return payload.GetProperty("access_token").GetString()
                    ?? throw new InvalidOperationException("Keycloak did not return an access token.");
            }
            catch (HttpRequestException) when (!startupCancellation.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), startupCancellation.Token);
            }
        }
    }
}
