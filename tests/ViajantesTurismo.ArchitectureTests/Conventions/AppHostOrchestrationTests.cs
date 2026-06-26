using static ViajantesTurismo.ArchitectureTests.Conventions.AppHostOrchestrationTestsHelpers;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

public sealed partial class AppHostOrchestrationTests
{
    [Fact]
    public void Catalog_Api_Should_Not_Wait_For_Postgres_While_It_Uses_InMemory_Read_Model_Storage()
    {
        // Arrange
        var appHostText = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "ViajantesTurismo.AppHost", "AppHost.cs"));

        // Act
        var catalogApiBlock = CatalogApiResourceRegex().Match(appHostText).Value;

        // Assert
        Assert.NotEmpty(catalogApiBlock);
        Assert.DoesNotContain("WithReference(database)", catalogApiBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("WaitFor(database)", catalogApiBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("WaitForCompletion(migrationService)", catalogApiBlock, StringComparison.Ordinal);
    }

}
