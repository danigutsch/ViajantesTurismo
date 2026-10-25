namespace ViajantesTurismo.ArchitectureTests.Repository;

/// <summary>
/// A Git repository that holds the solution, the project and build files, and the CI configuration of this repository,
/// so CI selection can run against one committed change.
/// </summary>
internal sealed class CiSelectionFixture : IDisposable
{
    private static readonly string[] SelectionInputs = ["ViajantesTurismo.slnx", ".config/sharedkernel-ci.json", "*.csproj", "*.props", "*.targets"];

    private readonly string _repositoryRoot = RepositoryTestProcess.GetRepositoryRoot();

    public CiSelectionFixture()
    {
        RootPath = Path.Combine(Path.GetTempPath(), "viajantes-ci-selection", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(RootPath);
    }

    public string RootPath
    {
        get;
    }

    /// <summary>Commits the selection inputs, then <paramref name="changedPath"/>, and returns the GitHub outputs of the selection.</summary>
    public async Task<Dictionary<string, string>> Select(string changedPath, CancellationToken ct)
    {
        var files = await Run("git", _repositoryRoot, ["ls-files", "-z", "--", .. SelectionInputs], ct);
        foreach (var file in files.Split('\0', StringSplitOptions.RemoveEmptyEntries))
        {
            var target = Path.Combine(RootPath, file);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(Path.Combine(_repositoryRoot, file), target);
        }

        await Run("git", RootPath, ["init", "--quiet"], ct);
        var baseline = await Commit(ct);
        var changed = Path.Combine(RootPath, changedPath);
        Directory.CreateDirectory(Path.GetDirectoryName(changed)!);
        await File.AppendAllTextAsync(changed, "// changed\n", ct);
        await Commit(ct);

        var outputs = Path.Combine(RootPath, "github-output.txt");
        await Run("dotnet", _repositoryRoot,
            ["tool", "run", "sharedkernel-ci", "--", "select", "--base", baseline, "--root", RootPath, "--selection", Path.Combine(RootPath, "selection"), "--github-output", outputs], ct);
        return (await File.ReadAllLinesAsync(outputs, ct))
            .Select(static line => line.Split('=', 2))
            .ToDictionary(static pair => pair[0], static pair => pair[1], StringComparer.Ordinal);
    }

    public void Dispose()
    {
        TemporaryRepositoryDirectory.Delete(RootPath);
    }

    private async Task<string> Commit(CancellationToken ct)
    {
        await Run("git", RootPath, ["add", "--all"], ct);
        await Run("git", RootPath,
        [
            "-c", "commit.gpgsign=false", "-c", "core.hooksPath=" + Path.Combine(RootPath, ".git", "no-hooks"),
            "-c", "user.name=CI Selection Tests", "-c", "user.email=ci-selection@example.invalid",
            "commit", "--quiet", "--allow-empty", "-m", "fixture",
        ], ct);
        return (await Run("git", RootPath, ["rev-parse", "HEAD"], ct)).Trim();
    }

    private static async Task<string> Run(string fileName, string workingDirectory, IReadOnlyList<string> arguments, CancellationToken ct)
    {
        var result = await RepositoryTestProcess.Run(fileName, workingDirectory, arguments, ct);
        result.ExitCode.ShouldBe(0, $"{fileName} {string.Join(' ', arguments)}: {result.StandardOutput}{result.StandardError}");
        return result.StandardOutput;
    }
}
