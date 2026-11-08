using System.Text.Json;
using System.Xml.Linq;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>Reads the lock file policy inputs: the projects, the lock-exempt list and the lock files.</summary>
internal static class LockFilePolicyFiles
{
    /// <summary>Gets the repository root.</summary>
    public static string Root => AnalyzerSuppressionPolicyTestsHelpers.GetRepositoryRoot();

    /// <summary>Gets the repository-relative project paths that <c>lockExemptProjects</c> lists, in file order.</summary>
    public static string[] ReadExemptProjects()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, ".config", "sharedkernel-repo.json")));
        return document.RootElement.TryGetProperty("lockExemptProjects", out var list)
            ? [.. list.EnumerateArray().Select(project => project.GetString()!)]
            : [];
    }

    /// <summary>Gets the repository-relative paths of every project file, outside generated directories.</summary>
    public static string[] ReadProjects() =>
    [
        .. AnalyzerSuppressionPolicyTestsHelpers.EnumerateRepositoryFiles(Root, "*.csproj")
            .Select(path => Path.GetRelativePath(Root, path).Replace('\\', '/'))
            .Order(StringComparer.Ordinal)
    ];

    /// <summary>Indicates whether the directory of <paramref name="project"/> holds a <c>packages.lock.json</c>.</summary>
    public static bool HasLockFile(string project) =>
        File.Exists(Path.Combine(Root, Path.GetDirectoryName(project)!, "packages.lock.json"));

    /// <summary>Gets the <c>RestorePackagesWithLockFile</c> value that the project file sets, or <see langword="null"/>.</summary>
    public static string? ReadRestoreWithLockFile(string project) =>
        XDocument.Load(Path.Combine(Root, project)).Descendants("RestorePackagesWithLockFile").LastOrDefault()?.Value;

    /// <summary>Gets the <c>name/version</c> pairs of the packages that the lock file of <paramref name="project"/> resolves.</summary>
    public static string[] ReadLockedPackages(string project)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, Path.GetDirectoryName(project)!, "packages.lock.json")));
        return
        [
            .. document.RootElement.GetProperty("dependencies").EnumerateObject()
                .SelectMany(framework => framework.Value.EnumerateObject())
                .Where(dependency => dependency.Value.GetProperty("type").GetString() != "Project")
                .Select(dependency => $"{dependency.Name}/{dependency.Value.GetProperty("resolved").GetString()}")
        ];
    }

    /// <summary>Gets the <c>name/version</c> libraries that the restored assets of <paramref name="project"/> resolve.</summary>
    public static string[] ReadResolvedPackages(string project)
    {
        var assets = Path.Combine(Root, Path.GetDirectoryName(project)!, "obj", "project.assets.json");
        if (!File.Exists(assets))
        {
            return [];
        }

        using var document = JsonDocument.Parse(File.ReadAllText(assets));
        return
        [
            .. document.RootElement.GetProperty("targets").EnumerateObject()
                .SelectMany(target => target.Value.EnumerateObject())
                .Where(library => library.Value.GetProperty("type").GetString() == "package")
                .Select(library => library.Name)
        ];
    }

    /// <summary>Indicates whether the restored assets of <paramref name="project"/> exist.</summary>
    public static bool HasRestoreAssets(string project) =>
        File.Exists(Path.Combine(Root, Path.GetDirectoryName(project)!, "obj", "project.assets.json"));

    /// <summary>Indicates whether <paramref name="project"/> is an application, worker, AppHost or other executable.</summary>
    public static bool IsExecutable(string project)
    {
        var document = XDocument.Load(Path.Combine(Root, project));
        var sdk = document.Root?.Attribute("Sdk")?.Value ?? string.Empty;
        var outputType = document.Descendants("OutputType").LastOrDefault()?.Value;
        return sdk is not "Microsoft.NET.Sdk" and not "Microsoft.NET.Sdk.Razor"
            || outputType is "Exe" or "WinExe";
    }
}
