using System.Net;
using Microsoft.AspNetCore.WebUtilities;

namespace ViajantesTurismo.Management.WebTests;

internal sealed class RecordingAudienceTokenEndpointHandler : HttpMessageHandler
{
    public List<IReadOnlyDictionary<string, string>> Requests { get; } = [];

    public List<string?> ClientAuthorizationHeaders { get; } = [];

    public Func<IReadOnlyDictionary<string, string>, HttpResponseMessage> ResponseFactory { get; set; } = CreateSuccessResponse;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.Content);

        var formContent = await request.Content.ReadAsStringAsync(cancellationToken);
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
