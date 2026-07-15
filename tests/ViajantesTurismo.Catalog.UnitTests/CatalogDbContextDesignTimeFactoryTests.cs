using Microsoft.EntityFrameworkCore;
using Npgsql;
using SharedKernel.Testing;
using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.Catalog.UnitTests;

[Trait(SharedKernelTestTraitNames.CategoryName, "unit")]
public sealed class CatalogDbContextDesignTimeFactoryTests
{
    [Fact]
    public void Design_time_factory_configures_catalog_postgresql_provider()
    {
        // Arrange
        var factory = new CatalogDbContextDesignTimeFactory();

        // Act
        using var dbContext = factory.CreateDbContext([]);
        var providerName = dbContext.Database.ProviderName;
        var connectionString = dbContext.Database.GetDbConnection().ConnectionString;
        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString);

        // Assert
        providerName.ShouldBe("Npgsql.EntityFrameworkCore.PostgreSQL");
        connectionStringBuilder.Host.ShouldBe("localhost");
        connectionStringBuilder.Database.ShouldBe("catalog-design-time");
    }
}
