using System.Text.Json.Nodes;

namespace ViajantesTurismo.RepoConfig.Tool;

internal static class BootstrapSharedKernelCommand
{
    public static async Task<int> Run(string[] args, TextWriter output, string workingDirectory, CancellationToken ct)
    {
        var root = workingDirectory;
        string? library = null;
        string? feed = null;
        for (var index = 0; index < args.Length; index += 2)
        {
            if (index + 1 == args.Length) { throw new ArgumentException("Missing value for " + args[index]); }
            switch (args[index])
            {
                case "--root": root = Path.GetFullPath(args[index + 1]); break;
                case "--library-root": library = Path.GetFullPath(args[index + 1]); break;
                case "--feed": feed = Path.GetFullPath(args[index + 1]); break;
                default: throw new ArgumentException("Unknown option: " + args[index]);
            }
        }

        if (library is null) { throw new ArgumentException("--library-root is required."); }
        var candidate = Path.Combine(root, ".config", "sharedkernel-candidate.json");
        var pin = JsonNode.Parse(await File.ReadAllTextAsync(candidate, ct).ConfigureAwait(false))!.AsObject();
        var revision = await CandidateCompatibilityCommand.RunProcess("git", ["rev-parse", "HEAD"], library, TextWriter.Null, ct).ConfigureAwait(false);
        var dirty = await CandidateCompatibilityCommand.RunProcess("git", ["status", "--porcelain"], library, TextWriter.Null, ct).ConfigureAwait(false);
        if (dirty.Length != 0 || revision != pin["libraryRevision"]?.GetValue<string>())
        {
            throw new ArgumentException("Bootstrap requires a clean checkout of the pinned SharedKernel revision.");
        }

        feed ??= Path.Combine(library, "artifacts", "api-compat", "packages", pin["packageVersion"]!.GetValue<string>());
        var tool = Path.Combine(library, "tools", "SharedKernel.Versioning.Tool", "SharedKernel.Versioning.Tool.csproj");
        await CandidateCompatibilityCommand.RunProcess("dotnet",
            ["run", "--project", tool, "-c", "Release", "--", "stage-candidate", "--repo-root", library,
                "--candidate", candidate, "--feed", feed, "--output", Path.Combine(root, ".local", "sharedkernel-feed")],
            library, output, ct).ConfigureAwait(false);
        return 0;
    }
}
