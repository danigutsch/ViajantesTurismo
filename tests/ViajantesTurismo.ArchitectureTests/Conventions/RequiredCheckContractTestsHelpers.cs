namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>Helpers for the tests of the required check contract between governance docs and CI workflows.</summary>
internal static class RequiredCheckContractTestsHelpers
{
    private const string IgnoredLabelName = "github.event.action == 'labeled' && github.event.label.name != 'ci:rerun' && 'Ignored label event' || ";
    private const string RerunCondition = "github.event.action != 'labeled' || github.event.label.name == 'ci:rerun'";
    private const string IgnoredLabelConcurrency = "github.event.action == 'labeled' && github.event.label.name != 'ci:rerun' && github.run_id";

    /// <summary>Reads the required checks listed in the governance document as (check name, workflow path) pairs.</summary>
    public static IReadOnlyList<(string Name, string Workflow)> ReadRequiredChecks(string governancePath) =>
        File.ReadLines(governancePath)
            .Where(static line => line.StartsWith("- `", StringComparison.Ordinal)
                && line.Contains("` (from `.github/workflows/", StringComparison.Ordinal))
            .Select(static line => (line.Split('`')[1], line.Split('`')[3]))
            .ToArray();

    /// <summary>
    /// Returns why the workflow job that reports a required check breaks the ignored-label contract, or null.
    /// </summary>
    public static string? VerifyRequiredCheck(string root, string name, string workflowPath)
    {
        var workflow = File.ReadAllText(Path.Combine(root, workflowPath)).ReplaceLineEndings("\n");
        var lines = workflow.Split('\n');
        var nameIndex = Array.IndexOf(lines, $"    name: ${{{{ {IgnoredLabelName}'{name}' }}}}");
        if (nameIndex < 0)
        {
            return $"{workflowPath} has no job named '{name}' with the ignored-label name expression.";
        }

        var start = Array.FindLastIndex(lines, nameIndex, IsJobKey);
        var end = Array.FindIndex(lines, nameIndex + 1, IsJobKey);
        var job = lines[start..(end < 0 ? lines.Length : end)];
        var guarded = job.Any(static line => line == $"    if: ${{{{ {RerunCondition} }}}}"
            || line.StartsWith($"    if: ${{{{ ({RerunCondition}) && ", StringComparison.Ordinal));
        if (!guarded)
        {
            return $"{workflowPath} job '{name}' does not start its if: with the ci:rerun label condition.";
        }

        return workflow.Contains(IgnoredLabelConcurrency, StringComparison.Ordinal)
            ? null
            : $"{workflowPath} does not move ignored label events to their own concurrency group.";
    }

    private static bool IsJobKey(string line) =>
        line.Length > 2 && line.StartsWith("  ", StringComparison.Ordinal) && line[2] is not ' ' and not '#' && line.EndsWith(':');
}
