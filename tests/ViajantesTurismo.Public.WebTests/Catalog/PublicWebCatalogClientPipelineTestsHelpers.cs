using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ViajantesTurismo.Public.WebTests.Catalog;

/// <summary>Helpers for inspecting the outbound HttpClient handler pipeline of Public Web.</summary>
internal static class PublicWebCatalogClientPipelineTestsHelpers
{
    private const string CatalogClientName = nameof(IPublicCatalogApiClient);

    /// <summary>Lists the full type names of the Catalog client's delegating handlers, outermost first.</summary>
    public static List<string> CatalogHandlerTypeNames(WebApplicationFactory<IPublicWebAssemblyMarker> factory)
    {
        var handler = factory.Services.GetRequiredService<IHttpMessageHandlerFactory>().CreateHandler(CatalogClientName);
        var names = new List<string>();
        for (var current = handler as DelegatingHandler; current is not null; current = current.InnerHandler as DelegatingHandler)
        {
            names.Add(current.GetType().FullName!);
        }

        return names;
    }

    /// <summary>Creates the named Catalog HTTP client from the hosted application.</summary>
    public static HttpClient CreateCatalogClient(WebApplicationFactory<IPublicWebAssemblyMarker> factory) =>
        factory.Services.GetRequiredService<IHttpClientFactory>().CreateClient(CatalogClientName);
}
