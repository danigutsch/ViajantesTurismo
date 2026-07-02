using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Testing.Assertions;
using ViajantesTurismo.Catalog.Application.Media;
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
        provider.GetRequiredService<CatalogDbContext>().ShouldNotBeNull();
        provider.GetRequiredService<IPublicContentStore>().ShouldBeOfType<EfPublicContentStore>();
        provider.GetRequiredService<IMediaObjectStore>().ShouldBeOfType<LocalMediaObjectStore>();
        provider.GetRequiredService<IMediaUploadScanner>().ShouldBeOfType<NoOpMediaUploadScanner>();
        provider.GetRequiredService<IMediaUploadValidator>().ShouldBeOfType<MediaUploadValidator>();
    }
}
