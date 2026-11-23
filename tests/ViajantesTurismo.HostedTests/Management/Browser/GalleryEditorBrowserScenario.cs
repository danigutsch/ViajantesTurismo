using Microsoft.Extensions.DependencyInjection;
using ViajantesTurismo.Catalog.Contracts.Http;

namespace ViajantesTurismo.HostedTests.Management.Browser;

/// <summary>Starts the actual Management gallery Razor route on the loopback test host.</summary>
internal static class GalleryEditorBrowserScenario
{
    /// <summary>Starts the host for a synthetic Catalog image library.</summary>
    public static Task<ManagementWebBrowserHost> Start(GalleryEditorCatalogStub catalog, CancellationToken ct)
    {
        IServiceCollection registrations = new ServiceCollection();
        Register(registrations, catalog);
        return ManagementWebBrowserHost.Start(registrations, ct);
    }

    /// <summary>Uses the real clients against the scripted Catalog responses.</summary>
    internal static void Register(IServiceCollection registrations, GalleryEditorCatalogStub catalog)
    {
        registrations.AddHttpClient<ICatalogGalleryEditorApiClient, CatalogGalleryEditorApiClient>(client => client.BaseAddress = new Uri("https://catalog.test"))
            .ConfigurePrimaryHttpMessageHandler(() => catalog);
        registrations.AddHttpClient<ICatalogToursApiClient, CatalogToursApiClient>(client => client.BaseAddress = new Uri("https://catalog.test"))
            .ConfigurePrimaryHttpMessageHandler(() => catalog);
    }
}
