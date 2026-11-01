using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Catalog.Domain.PublicContent;
using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.Catalog.UnitTests;

/// <summary>Protects the unchanged storage representation of typed public-content identities.</summary>
public sealed class PublicContentKeyConfigurationTests
{
    [Fact]
    public void Persisted_factory_identity_survives_conversion_when_uppercasing_changes_normalization()
    {
        // Arrange
        using var dbContext = CatalogDbContextTestFactory.Create();
        var entity = dbContext.Model.FindEntityType(typeof(EditablePublicContent)).ShouldNotBeNull();
        var property = entity.FindProperty(nameof(EditablePublicContent.ContentKey)).ShouldNotBeNull();
        var converter = property.GetValueConverter().ShouldNotBeNull();
        var key = PublicContentKey.Create("home.i\u0307").Value;

        // Act
        var stored = converter.ConvertToProvider(key);
        var loaded = converter.ConvertFromProvider(stored);

        // Assert
        stored.ShouldBe("HOME.I\u0307");
        loaded.ShouldBe(key);
    }

    [Fact]
    public void Typed_key_queries_translate_to_postgresql_without_a_schema_change()
    {
        // Arrange
        using var dbContext = new CatalogDbContextDesignTimeFactory().CreateDbContext([]);
        var key = PublicContentKey.Create("home.hero").Value;

        // Act
        var sql = dbContext.PublicContent.Where(content => content.ContentKey == key).ToQueryString();
        var hasSchemaChanges = dbContext.Database.HasPendingModelChanges();

        // Assert
        sql.ShouldContain("\"Key\"", StringComparison.Ordinal);
        sql.ShouldContain("HOME.HERO", StringComparison.Ordinal);
        hasSchemaChanges.ShouldBeFalse();
    }

    [Fact]
    public void Canonical_key_conversion_preserves_the_existing_column_and_unique_index()
    {
        // Arrange
        using var dbContext = CatalogDbContextTestFactory.Create();
        var entity = dbContext.Model.FindEntityType(typeof(EditablePublicContent)).ShouldNotBeNull();
        var property = entity.FindProperty(nameof(EditablePublicContent.ContentKey)).ShouldNotBeNull();
        var converter = property.GetValueConverter().ShouldNotBeNull();
        var key = PublicContentKey.Create("home.hero").Value;

        // Act
        var stored = converter.ConvertToProvider(key);
        var loaded = converter.ConvertFromProvider("HOME.HERO");
        var index = entity.GetIndexes().ShouldHaveSingleItem(candidate => candidate.Properties.Contains(property));

        // Assert
        stored.ShouldBe("HOME.HERO");
        loaded.ShouldBe(key);
        property.FindAnnotation("Relational:ColumnName").ShouldNotBeNull().Value.ShouldBe("Key");
        index.IsUnique.ShouldBeTrue();
        index.FindAnnotation("Relational:Name").ShouldNotBeNull().Value.ShouldBe("IX_PublicContent_Key");
        entity.FindProperty(nameof(EditablePublicContent.Key)).ShouldBeNull();
    }
}
