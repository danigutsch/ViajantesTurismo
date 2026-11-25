using Microsoft.Extensions.Http.Resilience;
using SharedKernel.AspNetCore;
using SharedKernel.HttpClients;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Public.Web.Catalog;

/// <summary>Configures the anonymous Catalog client used by the public site.</summary>
internal static class PublicCatalogClientServiceCollectionExtensions
{
    /// <summary>Registers the public Catalog client with Aspire discovery and forwarded client IP.</summary>
    internal static IServiceCollection AddPublicCatalogClient(this IServiceCollection services)
    {
        services.AddHttpClient<IPublicCatalogApiClient, PublicCatalogApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.CatalogApi}"))
            .AddForwardedClientIp();
        return services;
    }

    /// <summary>Stops retries and circuit-breaker counting for public Catalog quota (429) responses.</summary>
    internal static void ConfigureResilience(HttpStandardResilienceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.DisableRetryOnTooManyRequests(static request => request.RequestUri is { IsAbsoluteUri: true } uri
            && uri.AbsolutePath.StartsWith(PublicCatalogApiClient.RoutePrefix + "/", StringComparison.OrdinalIgnoreCase));
    }
}
