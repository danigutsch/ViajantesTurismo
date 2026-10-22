namespace ViajantesTurismo.RepoConfig.Tool;

internal static class CiRestoreCommand
{
    public static async Task<int> Run(string[] args, TextWriter output, string root, CancellationToken ct)
    {
        string? manifest = null, targets = null;
        for (var index = 0; index < args.Length; index += 2)
        {
            if (index + 1 == args.Length)
            {
                throw new ArgumentException("Missing restore option value.");
            }
            switch (args[index])
            {
                case "--projects-file":
                    manifest = args[index + 1];
                    break;
                case "--targets":
                    targets = args[index + 1];
                    break;
                default:
                    throw new ArgumentException("Unknown restore option.");
            }
        }
        var projects = (!string.IsNullOrEmpty(manifest)
            ? await File.ReadAllLinesAsync(CiProjectGraph.Resolve(root, manifest), ct).ConfigureAwait(false)
            : (targets ?? string.Empty).Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
            .Select(path => path.Trim()).Where(path => path.Length > 0).Distinct(StringComparer.Ordinal).ToArray();
        if (projects.Length == 0)
        {
            throw new ArgumentException("At least one restore target is required.");
        }
        foreach (var project in projects)
        {
            CiProjectGraph.Resolve(root, project);
        }
        if (projects.Length > 1 && projects.All(project => Path.GetExtension(project) is ".csproj" or ".fsproj" or ".vbproj"))
        {
            var directory = Path.Combine(root, "TestResults", "ci-restore." + Guid.NewGuid().ToString("N"));
            var graph = CiProjectGraph.Write(root, directory, "restore", projects);
            await CandidateCompatibilityCommand.RunProcess("dotnet", ["restore", graph, "--locked-mode"], root, output, ct).ConfigureAwait(false);
        }
        else
        {
            foreach (var project in projects)
            {
                await CandidateCompatibilityCommand.RunProcess("dotnet", ["restore", project, "--locked-mode"], root, output, ct).ConfigureAwait(false);
            }
        }
        return 0;
    }
}
