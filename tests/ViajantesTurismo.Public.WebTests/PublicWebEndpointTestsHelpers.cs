using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace ViajantesTurismo.Public.WebTests;

internal static class PublicWebEndpointTestsHelpers
{
    private const int MaximumProductionStartupAttempts = 3;

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
            IPublicCatalogApiClient? catalogApiClient = null,
            IBrandingApiClient? brandingApiClient = null,
            string? environment = null,
            string? canonicalOrigin = null)
    {
        IReadOnlyDictionary<string, string?>? configuration = canonicalOrigin is null
            ? null
            : new Dictionary<string, string?>
            {
                [$"{PublicWebSitemapOptions.SectionName}:CanonicalOrigin"] = canonicalOrigin
            };

        return WebApplicationTestHost.Create<IPublicWebAssemblyMarker>(
            environment,
            services =>
            {
                services.RemoveAll<IPublicCatalogApiClient>();
                services.RemoveAll<IBrandingApiClient>();
                services.AddSingleton(catalogApiClient ?? new FakePublicCatalogApiClient());
                services.AddSingleton(brandingApiClient ?? new FakeBrandingApiClient());
            },
            configuration: configuration);
    }

    public static OptionsValidationException GetProductionSitemapValidationException(string? canonicalOrigin = null)
    {
        for (var attempt = 1; attempt <= MaximumProductionStartupAttempts; attempt++)
        {
            using var factory = CreateFactory(environment: "Production", canonicalOrigin: canonicalOrigin);
            try
            {
                using var client = factory.CreateClient();
            }
            catch (OptionsValidationException exception)
            {
                return exception;
            }
            catch (ObjectDisposedException) when (attempt < MaximumProductionStartupAttempts)
            {
                // Retry the transient deferred-host disposal race without weakening the startup assertion.
            }
        }

        throw new InvalidOperationException("Production sitemap validation did not fail during startup.");
    }
}
