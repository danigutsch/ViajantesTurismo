using System.Net.Http.Headers;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure;

/// <summary>Sends Branding logo upload and read requests in tests.</summary>
internal static class BrandingLogoRequests
{
    private const string ManagementLogoPath = $"/api/v1/{BrandingRoutes.ManagementSettingsPath}/logo";
    private const string PublicLogoPathPrefix = $"/api/v1/{BrandingRoutes.PublicSettingsPath}/logo/";

    /// <summary>Sends a PUT of raw image bytes with the declared content type and optional If-Match values.</summary>
    public static Task<HttpResponseMessage> PutLogo(HttpClient client, byte[] content, string contentType, params string[] ifMatch)
    {
        ArgumentNullException.ThrowIfNull(client);

        var message = new HttpRequestMessage(HttpMethod.Put, new Uri(ManagementLogoPath, UriKind.Relative))
        {
            Content = new ByteArrayContent(content)
        };
        message.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        if (ifMatch.Length > 0)
        {
            message.Headers.TryAddWithoutValidation("If-Match", ifMatch).ShouldBeTrue();
        }

        return client.SendAsync(message, TestContext.Current.CancellationToken);
    }

    /// <summary>Sends a GET of the public logo with the given address and optional If-None-Match value.</summary>
    public static Task<HttpResponseMessage> GetLogo(HttpClient client, string address, string? ifNoneMatch = null)
    {
        ArgumentNullException.ThrowIfNull(client);

        var message = new HttpRequestMessage(HttpMethod.Get, new Uri(PublicLogoPathPrefix + address, UriKind.Relative));
        if (ifNoneMatch is not null)
        {
            message.Headers.TryAddWithoutValidation("If-None-Match", ifNoneMatch).ShouldBeTrue();
        }

        return client.SendAsync(message, TestContext.Current.CancellationToken);
    }
}
