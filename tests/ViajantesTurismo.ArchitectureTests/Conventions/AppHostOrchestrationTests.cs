using System.Text.RegularExpressions;

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

    [GeneratedRegex(@"var\s+catalogApiService\s*=\s*builder\.AddProject<[^;]+;", RegexOptions.CultureInvariant)]
    private static partial Regex CatalogApiResourceRegex();

    private static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ViajantesTurismo.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not locate repository root.");
    }
}
