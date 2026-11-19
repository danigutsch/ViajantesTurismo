using PandaBear.Branding;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Public.Web.Branding;

/// <summary>Configures the anonymous Branding client for the public site.</summary>
internal static class PublicBrandingServiceCollectionExtensions
{
    /// <summary>Registers the client that loads the public theme from Catalog.</summary>
    internal static IServiceCollection AddPublicBranding(this IServiceCollection services)
    {
        services.AddHttpClient<IBrandingApiClient, BrandingApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.CatalogApi}"));
        return services;
    }
}
