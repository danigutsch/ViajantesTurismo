using Microsoft.EntityFrameworkCore;
using PandaBear.Testing;
using ViajantesTurismo.Branding.Infrastructure;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.PersistenceCategory)]
[TestScope(TestScope.Unit)]
public sealed class ClientGeneratedKeyConventionTests
{
    [Fact]
    public void The_scanner_reports_a_store_keyed_entity_and_ignores_a_client_keyed_one()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<StoreKeyedFixtureDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        using var context = new StoreKeyedFixtureDbContext(options);

        // Act
        var violations = StoreGeneratedKeyScanner.FindGeneratedKeys(context.Model);

        // Assert
        violations.ShouldBe([$"{nameof(StoreKeyedRow)}.{nameof(StoreKeyedRow.Id)}"]);
    }

    [Fact]
    public async Task The_admin_model_has_only_client_generated_keys()
    {
        // Arrange
        await using var scope = AdminWriteDbContextTestFactory.CreateWithGeneratedIntegrationEventDispatcher();

        // Act
        var violations = StoreGeneratedKeyScanner.FindGeneratedKeys(scope.DbContext.Model);

        // Assert
        violations.ShouldBeEmpty();
    }

    [Fact]
    public void The_branding_model_has_only_client_generated_keys()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<BrandingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        using var context = new BrandingDbContext(options);

        // Act
        var violations = StoreGeneratedKeyScanner.FindGeneratedKeys(context.Model);

        // Assert
        violations.ShouldBeEmpty();
    }
}
