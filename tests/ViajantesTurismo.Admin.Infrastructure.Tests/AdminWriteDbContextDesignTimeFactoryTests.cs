using Microsoft.EntityFrameworkCore;
using Npgsql;
using SharedKernel.Testing;

namespace ViajantesTurismo.Admin.Infrastructure.Tests;

[Trait(SharedKernelTestTraitNames.CategoryName, "unit")]
public sealed class AdminWriteDbContextDesignTimeFactoryTests
{
    [Fact]
    public void Design_time_factory_configures_admin_postgresql_provider()
    {
        // Arrange
        var factory = new AdminWriteDbContextDesignTimeFactory();

        // Act
        using var dbContext = factory.CreateDbContext([]);
        var providerName = dbContext.Database.ProviderName;
        var connectionString = dbContext.Database.GetDbConnection().ConnectionString;
        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString);

        // Assert
        providerName.ShouldBe("Npgsql.EntityFrameworkCore.PostgreSQL");
        connectionStringBuilder.Host.ShouldBe("localhost");
        connectionStringBuilder.Database.ShouldBe("admin-design-time");
    }
}
