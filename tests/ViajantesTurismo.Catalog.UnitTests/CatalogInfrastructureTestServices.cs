using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Catalog.UnitTests;

internal static class CatalogInfrastructureTestServices
{
    public static ServiceProvider CreateProvider()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{ResourceNames.Database}"] = "Host=localhost;Database=viajantes;Username=test;Password=test"
        });

        builder.AddCatalogInfrastructure();

        return builder.Services.BuildServiceProvider();
    }
}
