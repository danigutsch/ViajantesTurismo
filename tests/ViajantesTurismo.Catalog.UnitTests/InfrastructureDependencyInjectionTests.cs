using SharedKernel.Testing;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Application.PublicContent;
using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.Catalog.UnitTests;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraitValues.DependencyInjectionCategory)]
public sealed class InfrastructureDependencyInjectionTests
{
    [Fact]
    public void AddCatalogInfrastructure_registers_catalog_services()
    {
        // Arrange
        using var scenario = CatalogInfrastructureTestServices.CreateScenario();

        // Act

        // Assert
        scenario.ShouldResolve<CatalogDbContext>();
        scenario.ShouldResolveAs<IPublicContentStore, EfPublicContentStore>();
        scenario.ShouldResolveAs<IMediaObjectStore, LocalMediaObjectStore>();
        scenario.ShouldResolveAs<IMediaUploadScanner, NoOpMediaUploadScanner>();
        scenario.ShouldResolveAs<IMediaUploadValidator, MediaUploadValidator>();
    }
}
