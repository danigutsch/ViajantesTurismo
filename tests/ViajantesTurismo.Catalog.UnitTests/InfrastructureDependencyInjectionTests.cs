using Microsoft.Extensions.DependencyInjection;
using ViajantesTurismo.Catalog.Application.PublicContent;
using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class InfrastructureDependencyInjectionTests
{
    [Fact]
    public void AddCatalogInfrastructure_registers_catalog_services()
    {
        // Arrange
        // Act
        using var provider = CatalogInfrastructureTestServices.CreateProvider();

        // Assert
        Assert.NotNull(provider.GetRequiredService<CatalogDbContext>());
        Assert.IsType<EfPublicContentStore>(provider.GetRequiredService<IPublicContentStore>());
    }
}
