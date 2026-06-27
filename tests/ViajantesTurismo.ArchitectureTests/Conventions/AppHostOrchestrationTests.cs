using static ViajantesTurismo.ArchitectureTests.Conventions.AppHostOrchestrationTestsHelpers;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

public sealed partial class AppHostOrchestrationTests
{
    [Fact]
    public void Catalog_Api_Waits_For_Database_Migrations_When_It_Uses_Persisted_Public_Content()
    {
        // Arrange
        var appHostText = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "ViajantesTurismo.AppHost", "AppHost.cs"));

        // Act
        var catalogApiBlock = CatalogApiResourceRegex().Match(appHostText).Value;

        // Assert
        Assert.NotEmpty(catalogApiBlock);
        Assert.Contains("WithReference(database)", catalogApiBlock, StringComparison.Ordinal);
        Assert.Contains("WaitFor(database)", catalogApiBlock, StringComparison.Ordinal);
        Assert.Contains("WaitForCompletion(migrationService)", catalogApiBlock, StringComparison.Ordinal);
    }

}
