using SharedKernel.BuildingBlocks;

namespace ViajantesTurismo.Catalog.Api;

/// <summary>Builds relative Catalog navigation links from the registered routes.</summary>
internal static class CatalogEndpointLinks
{
    /// <summary>Creates a GET link to a named Catalog endpoint using explicit route values.</summary>
    internal static HypermediaLink Get(
        LinkGenerator routes, HttpContext context, string relation, string endpoint, RouteValueDictionary? values = null)
    {
        var href = routes.GetPathByName(context, endpoint, values ?? new RouteValueDictionary())
            ?? throw new InvalidOperationException($"Cannot generate a Catalog link to the named endpoint '{endpoint}'.");
        return new HypermediaLink(relation, href, HttpMethods.Get);
    }
}
