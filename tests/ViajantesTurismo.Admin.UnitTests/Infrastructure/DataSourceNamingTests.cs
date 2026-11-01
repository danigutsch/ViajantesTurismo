using PandaBear.Testing;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraitValues.DependencyInjectionCategory)]
public sealed class DataSourceNamingTests
{
    [Fact]
    public void Admin_infrastructure_names_the_shared_read_write_and_tour_capacity_lock_data_sources()
    {
        // Arrange
        using var scenario = DataSourceNamingScenario.ForAdmin();

        // Act
        var poolNames = NpgsqlPoolMetricTagCapture.ObservePoolNames();

        // Assert
        poolNames.ShouldContain("admin");
        poolNames.ShouldNotContain("admin-write");
        poolNames.ShouldNotContain("admin-read");
        poolNames.ShouldContain("admin-tour-capacity-lock");
    }

    [Fact]
    public void Admin_worker_infrastructure_names_the_branding_transport_data_source()
    {
        // Arrange
        using var scenario = DataSourceNamingScenario.ForAdminWorker();

        // Act
        var poolNames = NpgsqlPoolMetricTagCapture.ObservePoolNames();

        // Assert
        poolNames.ShouldContain("admin-branding-transport");
    }

    [Fact]
    public void Catalog_infrastructure_names_the_default_and_lock_data_sources()
    {
        // Arrange
        using var scenario = DataSourceNamingScenario.ForCatalog();

        // Act
        var poolNames = NpgsqlPoolMetricTagCapture.ObservePoolNames();

        // Assert
        poolNames.ShouldContain("catalog");
        poolNames.ShouldContain("catalog-lock");
    }

    [Fact]
    public void Catalog_transport_infrastructure_names_its_data_source()
    {
        // Arrange
        using var scenario = DataSourceNamingScenario.ForCatalogTransport();

        // Act
        var poolNames = NpgsqlPoolMetricTagCapture.ObservePoolNames();

        // Assert
        poolNames.ShouldContain("catalog-integration-transport");
    }

    [Fact]
    public void Branding_infrastructure_names_its_data_source()
    {
        // Arrange
        using var scenario = DataSourceNamingScenario.ForBranding();

        // Act
        var poolNames = NpgsqlPoolMetricTagCapture.ObservePoolNames();

        // Assert
        poolNames.ShouldContain("branding");
    }
}
