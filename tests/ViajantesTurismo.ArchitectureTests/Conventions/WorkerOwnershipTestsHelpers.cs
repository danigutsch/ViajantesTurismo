using System.Xml.Linq;
using ViajantesTurismo.ArchitectureTests.Dependencies;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

internal static class WorkerOwnershipTestsHelpers
{
    private static readonly string[] BoundedContexts = ["Admin", "Branding", "Catalog"];

    private static readonly string[] SharedHostProjects =
    [
        "ViajantesTurismo.Resources",
        "ViajantesTurismo.ServiceDefaults"
    ];

    public static string[] FindWorkerProjects()
    {
        var sourceRoot = Path.Combine(LayerDependencyTestsHelpers.GetRepositoryRoot(), "src");

        return Directory.EnumerateFiles(sourceRoot, "*.csproj", SearchOption.AllDirectories)
            .Select(Path.GetFileNameWithoutExtension)
            .OfType<string>()
            .Where(name => name.EndsWith("Worker", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    public static string? FindOwningContext(string projectName) =>
        BoundedContexts.FirstOrDefault(context =>
            projectName.StartsWith($"ViajantesTurismo.{context}.", StringComparison.Ordinal));

    public static string[] FindForeignContextReferences(string projectName, string owningContext)
    {
        var projectPath = Path.Combine(
            LayerDependencyTestsHelpers.GetRepositoryRoot(), "src", projectName, $"{projectName}.csproj");
        var ownPrefix = $"ViajantesTurismo.{owningContext}.";

        return XDocument.Load(projectPath)
            .Descendants("ProjectReference")
            .Select(reference => Path.GetFileNameWithoutExtension(
                (reference.Attribute("Include")?.Value ?? string.Empty).Replace('\\', '/')))
            .Where(reference => !reference.StartsWith(ownPrefix, StringComparison.Ordinal)
                                && !SharedHostProjects.Contains(reference, StringComparer.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();
    }
}
