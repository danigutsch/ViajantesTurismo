using System.Net;
using Microsoft.AspNetCore.WebUtilities;

namespace ViajantesTurismo.Management.WebTests;

/// <summary>Token endpoint handler that records form requests and returns a configurable response.</summary>
internal sealed class RecordingAudienceTokenEndpointHandler : HttpMessageHandler
{
    /// <summary>Parsed form fields of each token request.</summary>
    public List<IReadOnlyDictionary<string, string>> Requests { get; } = [];

    /// <summary>Authorization headers sent by the client on each token request, null when absent.</summary>
    public List<string?> ClientAuthorizationHeaders { get; } = [];

    /// <summary>Builds the response for a token request; defaults to a successful token for the requested audience.</summary>
    public Func<IReadOnlyDictionary<string, string>, HttpResponseMessage> ResponseFactory { get; set; } = CreateSuccessResponse;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request.Content);

        var formContent = await request.Content.ReadAsStringAsync(ct);
        var values = QueryHelpers.ParseQuery($"?{formContent}");
        var tokenRequest = values.ToDictionary(
            static pair => pair.Key,
            static pair => pair.Value.ToString(),
            StringComparer.Ordinal);

        Requests.Add(tokenRequest);
        ClientAuthorizationHeaders.Add(request.Headers.Authorization?.ToString());

        return ResponseFactory(tokenRequest);
    }

    private static HttpResponseMessage CreateSuccessResponse(IReadOnlyDictionary<string, string> request)
    {
        var audience = request["audience"];
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                $$"""{"access_token":"token-for-{{audience}}","expires_in":300,"token_type":"Bearer","issued_token_type":"urn:ietf:params:oauth:token-type:access_token"}""",
                System.Text.Encoding.UTF8,
                "application/json")
        };
    }
}
