using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Admin.Domain.Customers;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, "Regression")]
public sealed class CivilDatePersistenceTests
{
    [Theory]
    [InlineData(typeof(PersonalInfo), nameof(PersonalInfo.BirthDate))]
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

    [Fact]
    public void Birth_date_is_stored_as_a_date_and_restored_as_the_shared_birth_date()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AdminWriteDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only;Username=model_only")
            .Options;
        using var context = new AdminWriteDbContext(options);
        var property = context.Model.GetEntityTypes().ShouldHaveSingleItem(type => type.ClrType == typeof(PersonalInfo))
            .FindProperty(nameof(PersonalInfo.BirthDate)).ShouldNotBeNull();
        var converter = property.GetValueConverter().ShouldNotBeNull();
        var stored = new DateOnly(1990, 2, 28);

        // Act
        var restored = converter.ConvertFromProvider(stored);

        // Assert
        restored.ShouldBe(PandaBear.BuildingBlocks.BirthDate.Restore(stored));
        converter.ConvertToProvider(restored).ShouldBe(stored);
    }
}
