using System.Xml.Linq;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>Checks the declaration of the application repository's thin-tool analyzer opt-in.</summary>
internal static class ToolWrapperRegistrationPolicy
{
    private const string WrapperFlag = "ViajantesTurismoToolWrapper";
    private const string AnalyzerProject = "ViajantesTurismo.Architecture.Analyzers";

    /// <summary>Reports the wrapper project's registration errors.</summary>
    public static string[] Inspect(string root, string projectPath, XDocument project)
    {
        var relativePath = Path.GetRelativePath(root, projectPath).Replace('\\', '/');
        var diagnostics = new List<string>();
        if (!string.Equals(project.Descendants(WrapperFlag).LastOrDefault()?.Value, "true", StringComparison.OrdinalIgnoreCase))
        {
            diagnostics.Add($"{relativePath} must opt in to VTARCH004 with {WrapperFlag}=true.");
        }

        var compilerProperties = project.Descendants("CompilerVisibleProperty")
            .SelectMany(static element => element.Attribute("Include")?.Value.Split(';', StringSplitOptions.TrimEntries) ?? []);
        if (!compilerProperties.Contains(WrapperFlag, StringComparer.Ordinal))
        {
            diagnostics.Add($"{relativePath} must expose {WrapperFlag} through CompilerVisibleProperty for VTARCH004.");
        }

        var projectDirectory = Path.GetDirectoryName(projectPath) ?? root;
        var analyzerProject = Path.Combine(root, "src", AnalyzerProject, $"{AnalyzerProject}.csproj");
        if (!project.Descendants("ProjectReference").Any(reference => IsAnalyzerReference(reference, projectDirectory, analyzerProject)))
        {
            diagnostics.Add($"{relativePath} must include the {AnalyzerProject} analyzer project reference with OutputItemType=Analyzer and ReferenceOutputAssembly=false.");
        }

        return [.. diagnostics];
    }

    private static bool IsAnalyzerReference(XElement item, string projectDirectory, string analyzerProject) =>
        item.Attribute("Include")?.Value is { Length: > 0 } include &&
        string.Equals(Path.GetFullPath(include.Replace('\\', '/'), projectDirectory), analyzerProject, StringComparison.Ordinal) &&
        string.Equals(ReadMetadata(item, "OutputItemType"), "Analyzer", StringComparison.OrdinalIgnoreCase) &&
        string.Equals(ReadMetadata(item, "ReferenceOutputAssembly"), "false", StringComparison.OrdinalIgnoreCase);

    private static string? ReadMetadata(XElement item, string name) => item.Element(name)?.Value ?? item.Attribute(name)?.Value;
}
