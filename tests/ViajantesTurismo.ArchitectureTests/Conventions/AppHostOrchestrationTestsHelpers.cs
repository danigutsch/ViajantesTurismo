using System.Text.RegularExpressions;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>Helpers for tests that pin the AppHost orchestration source text.</summary>
internal static partial class AppHostOrchestrationTestsHelpers
{
    /// <summary>Returns the repository root: the nearest ancestor of the test output directory that holds ViajantesTurismo.slnx.</summary>
    public static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ViajantesTurismo.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not locate repository root.");
    }

    [GeneratedRegex(@"public\s+static\s+IResourceBuilder<ProjectResource>\s+AddCatalogApi[\s\S]+?;\s*\n\s*}", RegexOptions.CultureInvariant)]
    public static partial Regex CatalogApiResourceRegex();

    [GeneratedRegex(@"public\s+static\s+IResourceBuilder<ProjectResource>\s+AddDatabaseObservability[\s\S]+?;\s*\n\s*}", RegexOptions.CultureInvariant)]
    public static partial Regex DatabaseObservabilityResourceRegex();

    [GeneratedRegex(@"private\s+static\s+IResourceBuilder<ContainerResource>\s+AddIdentityProvider[\s\S]+?;\s*\n\s*}", RegexOptions.CultureInvariant)]
    public static partial Regex IdentityProviderResourceRegex();
}
