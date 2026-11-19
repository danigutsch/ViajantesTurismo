using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PandaBear.EventSourcing.Npgsql;
using PandaBear.Testing;
using ViajantesTurismo.ArchitectureDocumentation;
using static ViajantesTurismo.ArchitectureTests.Conventions.AdminTestArchitectureGuardTestsHelpers;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

[Trait(TestTraitNames.CategoryName, TestTraits.DocumentationCategory)]
public sealed class ArchitectureMetadataTests
{
    [Fact]
    public void The_generated_data_view_matches_the_architecture_metadata()
    {
        // Arrange
        var repository = GetRepositoryRoot();

        // Act
        var result = ArchitectureDataDiagram.Run(repository, checkOnly: true);

        // Assert
        result.ChangedFiles.ShouldBeEmpty();
    }

    [Fact]
    public void The_data_inventory_covers_every_application_DbContext()
    {
        // Arrange
        var repository = GetRepositoryRoot();
        var source = Path.Combine(repository, "docs", "architecture", "architecture-metadata.json");
        var actualContexts = ArchitectureMetadataSources.DbContexts()
            .Select(type => type.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        // Act
        using var metadata = JsonDocument.Parse(File.ReadAllText(source));
        var documentedContexts = metadata.RootElement.GetProperty("stores").EnumerateArray()
            .Select(store => store.GetProperty("dbContext").GetString())
            .OfType<string>()
            .Order(StringComparer.Ordinal)
            .ToArray();

        // Assert
        documentedContexts.ShouldBe(actualContexts);
    }

    [Theory]
    [InlineData("AdminWriteDbContext", "public", "Customers", "domain", "personal")]
    [InlineData("AdminWriteDbContext", "public", "Booking", "domain", "personal")]
    [InlineData("AdminWriteDbContext", "messaging", "outbox_messages", "outbox", "operational")]
    [InlineData("CatalogDbContext", "messaging", "idempotency_keys", "inbox", "operational")]
    [InlineData("CatalogDbContext", "public", "CatalogTourReadModels", "projection", "operational")]
    [InlineData("AdminWriteDbContext", "branding_projection", "inbox_messages", "inbox", "operational")]
    public void The_data_inventory_names_persisted_roles_and_data_classes(
        string context, string schema, string table, string role, string dataClass)
    {
        // Arrange
        var repository = GetRepositoryRoot();
        var source = Path.Combine(repository, "docs", "architecture", "architecture-metadata.json");
        using var metadata = JsonDocument.Parse(File.ReadAllText(source));
        var store = metadata.RootElement.GetProperty("stores").EnumerateArray()
            .ShouldHaveSingleItem(item => item.GetProperty("dbContext").GetString() == context);

        // Act
        var mapping = store.GetProperty("tables").EnumerateArray().ShouldHaveSingleItem(item =>
            item.GetProperty("schema").GetString() == schema && item.GetProperty("name").GetString() == table);
        var dataClasses = mapping.GetProperty("dataClasses").EnumerateArray().Select(item => item.GetString()).ToArray();

        // Assert
        mapping.GetProperty("role").GetString().ShouldBe(role);
        dataClasses.ShouldContain(dataClass);
    }

    [Fact]
    public void The_event_store_inventory_uses_the_registered_provider_schema()
    {
        // Arrange
        var repository = GetRepositoryRoot();
        var source = Path.Combine(repository, "docs", "architecture", "architecture-metadata.json");
        using var metadata = JsonDocument.Parse(File.ReadAllText(source));
        var eventStore = metadata.RootElement.GetProperty("stores").EnumerateArray()
            .ShouldHaveSingleItem(store => store.GetProperty("id").GetString() == "CatalogEventStore");
        var providerOptions = new PostgreSqlEventSourcingOptions();

        // Act
        var schemas = eventStore.GetProperty("tables").EnumerateArray()
            .Select(table => table.GetProperty("schema").GetString()).Distinct(StringComparer.Ordinal).ToArray();

        // Assert
        schemas.ShouldBe([providerOptions.Schema]);
    }

    [Theory]
    [InlineData("AdminReadDbContext", "AdminWriteDbContext")]
    [InlineData("AdminWriteDbContext", "AdminWriteDbContext")]
    [InlineData("AdminBrandingTransportDbContext", "CatalogDbContext")]
    [InlineData("CatalogDbContext", "CatalogDbContext")]
    [InlineData("CatalogIntegrationTransportDbContext", "AdminWriteDbContext")]
    [InlineData("BrandingDbContext", "BrandingDbContext")]
    [InlineData("ManagementSecurityDbContext", "ManagementSecurityDbContext")]
    public void The_data_inventory_resolves_its_EF_tables_against_the_owning_schema_model(
        string context, string schemaOwner)
    {
        // Arrange
        var repository = GetRepositoryRoot();
        var source = Path.Combine(repository, "docs", "architecture", "architecture-metadata.json");
        using var metadata = JsonDocument.Parse(File.ReadAllText(source));
        var store = metadata.RootElement.GetProperty("stores").EnumerateArray()
            .ShouldHaveSingleItem(item => item.GetProperty("dbContext").GetString() == context);
        var expected = store.GetProperty("tables").EnumerateArray()
            .Select(table => (Schema: table.GetProperty("schema").GetString(), Table: table.GetProperty("name").GetString()))
            .ToArray();

        // Act
        var actual = ArchitectureMetadataSources.Snapshot(schemaOwner).Model.GetEntityTypes()
            .Select(entity => (Schema: (string?)(entity.GetSchema() ?? "public"), Table: entity.GetTableName()))
            .ToHashSet();

        // Assert
        foreach (var mapping in expected)
        {
            actual.ShouldContain(mapping);
        }
    }

    [Fact]
    public void The_architecture_vocabulary_defines_boundaries_and_lifecycle_for_documented_components()
    {
        // Arrange
        var repository = GetRepositoryRoot();
        var source = Path.Combine(repository, "docs", "architecture", "architecture-metadata.json");
        using var metadata = JsonDocument.Parse(File.ReadAllText(source));
        var diagram = Path.Combine(repository, "docs", "architecture", "data-ownership.md");

        // Act
        var boundaries = metadata.RootElement.GetProperty("boundaries").EnumerateObject()
            .Select(boundary => boundary.Name).Order(StringComparer.Ordinal).ToArray();
        var lifecycle = metadata.RootElement.GetProperty("lifecycle").EnumerateObject()
            .Select(state => state.Name).Order(StringComparer.Ordinal).ToArray();
        var diagramExists = File.Exists(diagram);

        // Assert
        boundaries.ShouldBe(["data", "external", "internal", "management", "public"]);
        lifecycle.ShouldBe(["implemented", "planned"]);
        diagramExists.ShouldBeTrue();
    }
}
