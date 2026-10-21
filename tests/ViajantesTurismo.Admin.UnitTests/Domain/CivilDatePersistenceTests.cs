using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, "Regression")]
[Trait(PandaBear.Testing.TestTraitNames.ScopeName, PandaBear.Testing.SharedKernelTestTraitNames.UnitScope)]
public sealed class CivilDatePersistenceTests
{
    [Theory]
    [InlineData(typeof(PersonalInfo), nameof(PersonalInfo.BirthDate))]
    [InlineData(typeof(Payment), nameof(Payment.PaymentDate))]
    public void Civil_values_use_postgresql_date_columns(Type entityType, string propertyName)
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AdminWriteDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only;Username=model_only")
            .Options;
        using var context = new AdminWriteDbContext(options);
        // Act
        var property = context.Model.GetEntityTypes().ShouldHaveSingleItem(type => type.ClrType == entityType)
            .FindProperty(propertyName).ShouldNotBeNull();
        // Assert
        property.GetColumnType().ShouldBe("date");
    }
}
