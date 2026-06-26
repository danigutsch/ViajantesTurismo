using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ViajantesTurismo.Public.WebTests;

internal static class PublicWebEndpointTestsHelpers
{
    public static CatalogTourDto CreateTour(string slug, string title)
    {
        return new CatalogTourDto
        {
            Id = Guid.CreateVersion7(),
            AdminTourId = Guid.CreateVersion7(),
            Identifier = "TOUR-2026",
            Title = title,
            Slug = slug,
            IsPublished = true,
            Images = [],
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public static WebApplicationFactory<IPublicWebAssemblyMarker> CreateFactory(
            FakePublicCatalogApiClient? catalogApiClient = null,
            string? environment = null)
    {
        return WebApplicationTestHost.Create<IPublicWebAssemblyMarker>(
            environment,
            services =>
            {
                services.RemoveAll<IPublicCatalogApiClient>();
                services.AddSingleton<IPublicCatalogApiClient>(catalogApiClient ?? new FakePublicCatalogApiClient());
            });
    }
}
