using PandaBear.AspNetCore.Authentication.Bff;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Management.Web.Components.Pages.Catalog;

/// <summary>Composes the Management Web catalog workflows.</summary>
internal static class CatalogWorkflowServiceCollectionExtensions
{
    /// <summary>Registers the catalog tour and public content API clients.</summary>
    internal static void AddCatalogWorkflows(this IServiceCollection services)
    {
        services.AddHttpClient<ICatalogToursApiClient, CatalogToursApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.CatalogApi}"))
            .AddTokenExchangeHandler(ApiAudienceNames.Catalog, scope: ApiAudienceNames.Catalog);
        services.AddHttpClient<ICatalogGalleryEditorApiClient, CatalogGalleryEditorApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.CatalogApi}"))
            .AddTokenExchangeHandler(ApiAudienceNames.Catalog, scope: ApiAudienceNames.Catalog);
        services.AddHttpClient<IPublicContentApiClient, PublicContentApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.CatalogApi}"))
            .AddTokenExchangeHandler(ApiAudienceNames.Catalog, scope: ApiAudienceNames.Catalog);
    }
}
