using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using ViajantesTurismo.Catalog.Application;
using ViajantesTurismo.Catalog.Application.IntegrationEvents;

namespace ViajantesTurismo.Catalog.ApiServiceTests;

internal static class IntegrationEventOptionsTestServices
{
    public static IntegrationEventOptions GetConfiguredOptions(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddCatalogApplication();

        using var provider = services.BuildServiceProvider();

        return provider.GetRequiredService<IOptions<IntegrationEventOptions>>().Value;
    }
}
