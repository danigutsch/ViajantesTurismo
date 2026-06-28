using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using ViajantesTurismo.Catalog.Application;
using ViajantesTurismo.Catalog.Application.IntegrationEvents;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure;

internal static class IntegrationEventOptionsTestServices
{
    public static IntegrationEventOptions GetConfiguredOptions(TimeSpan idempotencyLockDuration)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{IntegrationEventOptions.SectionName}:IdempotencyLockDuration"] = idempotencyLockDuration.ToString("c")
            })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddCatalogApplication();

        using var provider = services.BuildServiceProvider();

        return provider.GetRequiredService<IOptions<IntegrationEventOptions>>().Value;
    }
}
