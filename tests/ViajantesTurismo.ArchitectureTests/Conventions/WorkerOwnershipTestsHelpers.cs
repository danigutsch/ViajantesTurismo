using System.Xml.Linq;
using ViajantesTurismo.ArchitectureTests.Dependencies;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>Helpers for the tests of worker project ownership by bounded context.</summary>
internal static class WorkerOwnershipTestsHelpers
{
    private static readonly string[] BoundedContexts = ["Admin", "Branding", "Catalog"];

    private static readonly string[] SharedHostProjects =
    [
        "ViajantesTurismo.Resources",
        "ViajantesTurismo.ServiceDefaults"
    ];

    /// <summary>Returns the sorted names of the source projects whose names end with "Worker".</summary>
    public static string[] FindWorkerProjects()
    {
        var sourceRoot = Path.Combine(LayerDependencyTestsHelpers.GetRepositoryRoot(), "src");

        return [.. Directory.EnumerateFiles(sourceRoot, "*.csproj", SearchOption.AllDirectories)
            .Select(Path.GetFileNameWithoutExtension)
            .OfType<string>()
            .Where(name => name.EndsWith("Worker", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)];
    }

    /// <summary>Returns the bounded context that owns the project, or null when none does.</summary>
    public static string? FindOwningContext(string projectName) =>
        BoundedContexts.FirstOrDefault(context =>
            projectName.StartsWith($"ViajantesTurismo.{context}.", StringComparison.Ordinal));

    // A worker consumes other contexts' published integration events; it never takes their other projects.
    private static bool IsPublishedIntegrationEventContract(string projectName) =>
        BoundedContexts.Any(context =>
            projectName == $"ViajantesTurismo.{context}.Contracts.IntegrationEvents");

    /// <summary>Returns the sorted project references of the worker that belong to neither its own context, the shared host projects nor a published integration event contract.</summary>
    public static string[] FindForeignContextReferences(string projectName, string owningContext)
    {
        var projectPath = Path.Combine(
            LayerDependencyTestsHelpers.GetRepositoryRoot(), "src", projectName, $"{projectName}.csproj");
        var ownPrefix = $"ViajantesTurismo.{owningContext}.";

        return [.. XDocument.Load(projectPath)
            .Descendants("ProjectReference")
            .Select(reference => Path.GetFileNameWithoutExtension(
                (reference.Attribute("Include")?.Value ?? string.Empty).Replace('\\', '/')))
            .Where(reference => !reference.StartsWith(ownPrefix, StringComparison.Ordinal)
                                && !SharedHostProjects.Contains(reference, StringComparer.Ordinal)
                                && !IsPublishedIntegrationEventContract(reference))
            .Order(StringComparer.Ordinal)];
    }
}
