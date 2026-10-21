using System.Text.Json;
using System.Xml.Linq;

namespace ViajantesTurismo.RepoConfig.Tool;

internal static class DependencyReviewCommand
{
    public static async Task<int> Run(string[] args, TextWriter output, string root, CancellationToken ct)
    {
        string? baseline = null, head = null, githubOutput = null;
        for (var index = 0; index < args.Length; index += 2)
        {
            if (index + 1 == args.Length) { throw new ArgumentException("Missing dependency review option value."); }
            switch (args[index])
            {
                case "--base": baseline = args[index + 1]; break;
                case "--head": head = args[index + 1]; break;
                case "--github-output": githubOutput = args[index + 1]; break;
                default: throw new ArgumentException("Unknown dependency review option.");
            }
        }
        var required = true;
        var reason = "Missing or uncertain comparison inputs require complete dependency review.";
        IReadOnlyList<string> changes = [];
        if (!string.IsNullOrEmpty(baseline) && !string.IsNullOrEmpty(head))
        {
            try
            {
                changes = await CiChangedPathReader.Read(root, baseline, head, useMergeBase: false, ct).ConfigureAwait(false);
                // Deliberately narrow: package READMEs, build files and source changes retain review.
                // An evaluated restore-input inventory is required before broadening this allowlist.
                required = changes.Any(path => !IsStandaloneDocumentation(path));
                if (!required) { required = !await HasKnownBuildImports(root, ct).ConfigureAwait(false); }
                reason = required ? "Dependency-affecting or unclassified inputs changed." : "Only standalone documentation changed; dependency inputs are unchanged.";
            }
            catch (Exception exception) when (exception is InvalidOperationException or TimeoutException or IOException or System.Xml.XmlException)
            {
                required = true;
                reason = "Comparison unavailable; complete dependency review is required.";
            }
        }
        var line = $"review_required={(required ? "true" : "false")}";
        await output.WriteLineAsync(line).ConfigureAwait(false);
        if (githubOutput is not null) { await File.AppendAllTextAsync(Path.GetFullPath(githubOutput, root), line + Environment.NewLine, ct).ConfigureAwait(false); }
        Directory.CreateDirectory(Path.Combine(root, "TestResults"));
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteString("base", baseline);
            writer.WriteString("head", head);
            writer.WriteBoolean("reviewRequired", required);
            writer.WriteString("reason", reason);
            writer.WriteStartArray("changedPaths");
            foreach (var path in changes) { writer.WriteStringValue(path); }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        await File.WriteAllBytesAsync(Path.Combine(root, "TestResults/dependency-review-plan.json"), buffer.ToArray(), ct).ConfigureAwait(false);
        return 0;
    }

    internal static bool IsStandaloneDocumentation(string path) =>
        (path.StartsWith("docs/", StringComparison.Ordinal) || path is "README.md" or "AGENTS.md" or "CONTRIBUTING.md" or "SECURITY.md")
        && Path.GetExtension(path) is ".md" or ".png" or ".jpg" or ".svg";

    private static async Task<bool> HasKnownBuildImports(string root, CancellationToken ct)
    {
        var inputs = await CandidateCompatibilityCommand.RunProcess("git", ["ls-files", "--", "*.csproj", "*.props", "*.targets"], root, TextWriter.Null, ct).ConfigureAwait(false);
        const string parentProps = "$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '$(MSBuildThisFileDirectory)../'))";
        foreach (var path in inputs.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var content = await File.ReadAllTextAsync(CiProjectGraph.Resolve(root, path), ct).ConfigureAwait(false);
            if (content.Contains("System.IO.File", StringComparison.Ordinal)) { return false; }
            var document = XDocument.Parse(content);
            if (document.Descendants().Any(element => element.Name.LocalName is "UsingTask" or "ReadLinesFromFile" or "Exec")) { return false; }
            foreach (var import in document.Descendants().Where(element => element.Name.LocalName == "Import"))
            {
                var project = import.Attribute("Project")?.Value;
                var sdk = import.Attribute("Sdk")?.Value;
                if (sdk == "Microsoft.NET.Sdk" && project is "Sdk.props" or "Sdk.targets") { continue; }
                if (project == parentProps && Path.GetFileName(path) == "Directory.Build.props") { continue; }
                return false;
            }
        }
        return true;
    }
}
