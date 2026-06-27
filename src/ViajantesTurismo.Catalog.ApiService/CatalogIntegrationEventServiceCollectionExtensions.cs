using SharedKernel.Configuration;
using ViajantesTurismo.Catalog.Application.IntegrationEvents;

namespace ViajantesTurismo.Catalog.ApiService;

internal static class CatalogIntegrationEventServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogIntegrationEvents(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services.AddValidatedOptions<IntegrationEventOptions, IntegrationEventOptionsValidator>(
            configuration.GetSection(IntegrationEventOptions.SectionName));
    }
}
