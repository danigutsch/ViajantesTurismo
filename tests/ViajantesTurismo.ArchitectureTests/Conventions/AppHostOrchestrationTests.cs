using static ViajantesTurismo.ArchitectureTests.Conventions.AppHostOrchestrationTestsHelpers;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

public sealed partial class AppHostOrchestrationTests
{
    [Fact]
    public void Catalog_api_waits_for_database_migrations_when_it_uses_persisted_public_content()
    {
        // Arrange
        var appHostText = File.ReadAllText(Path.Combine(
            GetRepositoryRoot(),
            "src",
            "ViajantesTurismo.AppHost",
            "AppHostResourceExtensions.cs"));

        // Act
        var catalogApiBlock = CatalogApiResourceRegex().Match(appHostText).Value;

        // Assert
        Assert.NotEmpty(catalogApiBlock);
        Assert.Contains("WithReference(database)", catalogApiBlock, StringComparison.Ordinal);
        Assert.Contains("WaitFor(database)", catalogApiBlock, StringComparison.Ordinal);
        Assert.Contains("WaitForCompletion(migrationService)", catalogApiBlock, StringComparison.Ordinal);
    }

}
