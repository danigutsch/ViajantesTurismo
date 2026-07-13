using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using ViajantesTurismo.Admin.UnitTests.Infrastructure;
using ViajantesTurismo.Management.Security;

namespace ViajantesTurismo.Admin.UnitTests.ManagementSecurity;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.SecurityCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.UnitScope)]
public sealed class ManagementSecurityDbContextFactoryTests
{
    [Fact]
    public void Creates_the_postgresql_security_context_with_the_expected_data_protection_table()
    {
        // Arrange
        var factory = new ManagementSecurityDbContextFactory();

        // Act
        using var context = factory.CreateDbContext([]);
        var entityType = context.Model.FindEntityType(typeof(DataProtectionKey));

        // Assert
        context.Database.ProviderName.ShouldBe("Npgsql.EntityFrameworkCore.PostgreSQL");
        var dataProtectionKeyEntity = entityType ?? throw new InvalidOperationException("The Data Protection key entity is not mapped.");
        dataProtectionKeyEntity.FindAnnotation("Relational:Schema")?.Value.ShouldBe(ManagementSecurityDefaults.SchemaName);
        dataProtectionKeyEntity.FindAnnotation("Relational:TableName")?.Value.ShouldBe("data_protection_keys");
    }
}
