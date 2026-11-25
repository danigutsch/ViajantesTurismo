using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Blazor.Site;
using ViajantesTurismo.Public.Web.Components.Shared;

namespace ViajantesTurismo.Public.WebTests.Components.Shared;

/// <summary>Provides routing and images for public media component tests.</summary>
internal static class PublicMediaComponentTestsHelpers
{
    /// <summary>Registers the public site's named media route for link generation.</summary>
    public static void AddMediaRoutes(IServiceCollection services)
    {
        services.AddRouting();
        services.AddSingleton<EndpointDataSource>(new DefaultEndpointDataSource(
            new RouteEndpoint(_ => Task.CompletedTask,
                RoutePatternFactory.Parse("/catalog/media/{id}/{width}/{format}"), 0,
                new EndpointMetadataCollection(new EndpointNameMetadata(PublicWebEndpoints.PublicMediaByRenditionEndpointName)), "media")));
    }

    /// <summary>Maps catalog images to the gallery images of <c>ImageGrid</c> with the test's link generator.</summary>
    public static IReadOnlyList<GalleryImage> Gallery(IServiceProvider services, IEnumerable<CatalogTourImageDto> images) =>
        TourImageRenditions.Gallery(services.GetRequiredService<LinkGenerator>(), images);
}
