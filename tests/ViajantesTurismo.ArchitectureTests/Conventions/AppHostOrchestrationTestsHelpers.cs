using System.Text.RegularExpressions;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

internal static partial class AppHostOrchestrationTestsHelpers
{
    public static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ViajantesTurismo.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not locate repository root.");
    }

    [GeneratedRegex(@"var\s+catalogApiService\s*=\s*builder\.AddProject<[^;]+;", RegexOptions.CultureInvariant)]
    public static partial Regex CatalogApiResourceRegex();
}
