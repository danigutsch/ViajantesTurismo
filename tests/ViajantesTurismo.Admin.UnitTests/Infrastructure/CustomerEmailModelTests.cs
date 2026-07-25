using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Infrastructure;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.PersistenceCategory)]
[Trait(SharedKernelTestTraitNames.ScopeName, TestTraits.UnitScope)]
public sealed class CustomerEmailModelTests
{
    [Fact]
    public void Customer_email_uses_case_insensitive_type_and_unique_index()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AdminWriteDbContext>()
            .UseNpgsql("Host=localhost;Database=customer-email-model")
            .Options;
        using var context = new AdminWriteDbContext(options);

        // Act
        var model = context.GetService<IDesignTimeModel>().Model;
        var contactInfoEntity = model.GetEntityTypes()
            .ShouldHaveSingleItem(entity => entity.ClrType == typeof(ContactInfo));
        var emailProperty = contactInfoEntity.FindProperty(nameof(ContactInfo.Email));
        var emailIndex = contactInfoEntity.GetIndexes()
            .ShouldHaveSingleItem(
                index => index.Properties.Count == 1 && index.Properties[0].Name == nameof(ContactInfo.Email));

        // Assert
        emailProperty.ShouldNotBeNull();
        emailProperty.GetColumnType().ShouldBe("citext");
        emailIndex.IsUnique.ShouldBeTrue();
        emailIndex.GetDatabaseName().ShouldBe("UX_CustomerContactInfo_Email");
    }
}
