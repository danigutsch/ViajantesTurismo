namespace SharedKernel.Http.Tests;

internal static class HttpClientDefaultsTestServices
{
    public static HttpClient CreateClient(string name)
    {
        var services = new ServiceCollection();
        services.AddHttpClientDefaults();

        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();

        return factory.CreateClient(name);
    }
}
