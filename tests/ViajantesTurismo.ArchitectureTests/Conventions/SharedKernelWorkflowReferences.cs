namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>Reads the SharedKernel references that the repository workflows and composite actions pin.</summary>
internal static class SharedKernelWorkflowReferences
{
    /// <summary>Lists the workflow and composite action files under <c>.github</c>.</summary>
    internal static string[] WorkflowFiles(string root) =>
        [.. Directory.EnumerateFiles(Path.Combine(root, ".github", "workflows"), "*.yml")
            .Concat(Directory.EnumerateFiles(Path.Combine(root, ".github", "actions"), "action.yml", SearchOption.AllDirectories))];

    /// <summary>Lists each <c>uses:</c> reference to the SharedKernel repository with its pinned revision.</summary>
    internal static (string Path, string Revision)[] SharedKernelReferences(string root) =>
        [.. WorkflowFiles(root)
            .SelectMany(File.ReadLines)
            .Select(static line => line.Trim().TrimStart('-', ' '))
            .Where(static line => line.StartsWith("uses: danigutsch/PandaBear.SharedKernel/", StringComparison.OrdinalIgnoreCase))
            .Select(static line => line["uses: ".Length..].Split('@'))
            .Select(static parts => (parts[0], parts[1].Split(' ')[0]))];
}
