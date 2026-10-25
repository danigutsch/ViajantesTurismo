namespace ViajantesTurismo.Branding.ApiServiceTests.Infrastructure;

internal static class BrandingManagementRequests
{
    private const string ManagementSettingsPath = $"/api/v1/{BrandingRoutes.ManagementSettingsPath}";

    public static Task<HttpResponseMessage> PutSettings(HttpClient client, BrandingSettingsDto request, params string[] ifMatch)
    {
        ArgumentNullException.ThrowIfNull(client);

        var message = new HttpRequestMessage(HttpMethod.Put, new Uri(ManagementSettingsPath, UriKind.Relative))
        {
            Content = JsonContent.Create(request)
        };
        if (ifMatch.Length > 0)
        {
            message.Headers.TryAddWithoutValidation("If-Match", ifMatch).ShouldBeTrue();
        }

        return client.SendAsync(message, TestContext.Current.CancellationToken);
    }

    public static Task<HttpResponseMessage> GetSettings(HttpClient client)
    {
        ArgumentNullException.ThrowIfNull(client);

        return client.GetAsync(new Uri(ManagementSettingsPath, UriKind.Relative), TestContext.Current.CancellationToken);
    }
}
