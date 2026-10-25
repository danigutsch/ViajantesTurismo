using static ViajantesTurismo.ArchitectureTests.Conventions.AdminTestArchitectureGuardTestsHelpers;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

public sealed class HostedTestSeamTests
{
    [Theory]
    [InlineData("Api")]
    [InlineData("System")]
    public void Http_and_browser_test_bodies_do_not_construct_persistence_adapters(string seam)
    {
        // Arrange
        var root = Path.Combine(GetRepositoryRoot(), "tests", "ViajantesTurismo.HostedTests", "Admin", seam);
        string[] persistenceConstructors = ["new NpgsqlConnection(", "new AdminWriteDbContext(", "new CatalogDbContext(", "new EfBrandingSettingsStore("];

        // Act
        var violations = Directory.GetFiles(root, "*Tests.cs", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedTestPath(path))
            .Where(path => persistenceConstructors.Any(constructor => File.ReadAllText(path).Contains(constructor, StringComparison.Ordinal)))
            .Select(path => Path.GetRelativePath(root, path))
            .ToArray();

        // Assert
        violations.ShouldBeEmpty();
    }
}
