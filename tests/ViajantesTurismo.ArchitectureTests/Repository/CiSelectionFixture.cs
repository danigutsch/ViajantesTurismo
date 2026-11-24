using PandaBear.Testing;

namespace ViajantesTurismo.ArchitectureTests.Repository;

/// <summary>
/// A Git repository that holds the solution, the project and build files, and the CI configuration of this repository,
/// so CI selection can run against one committed change.
/// </summary>
internal sealed class CiSelectionFixture : IDisposable
{
    private static readonly string[] SelectionInputs = ["ViajantesTurismo.slnx", ".config/sharedkernel-ci.json", "*.csproj", "*.props", "*.targets"];

    private readonly string _repositoryRoot = RepositoryRoot.Locate("ViajantesTurismo.slnx");
    private readonly TemporaryDirectory _directory = new();

    /// <summary>The temporary directory that holds the fixture repository.</summary>
    public string RootPath => _directory.Path;

    /// <summary>Commits the selection inputs, then <paramref name="changedPath"/>, and returns the GitHub outputs of the selection.</summary>
    public async Task<Dictionary<string, string>> Select(string changedPath, CancellationToken ct)
    {
        var files = await TestGit.Run(_repositoryRoot, ["ls-files", "-z", "--", .. SelectionInputs], ct);
        foreach (var file in files.Split('\0', StringSplitOptions.RemoveEmptyEntries))
        {
            var target = Path.Combine(RootPath, file);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(Path.Combine(_repositoryRoot, file), target);
        }

        var baseline = await TestGit.CommitAll(RootPath, ct);
        var changed = Path.Combine(RootPath, changedPath);
        Directory.CreateDirectory(Path.GetDirectoryName(changed)!);
        await File.AppendAllTextAsync(changed, "// changed\n", ct);
        await TestGit.Commit(RootPath, ct);

        var outputs = Path.Combine(RootPath, "github-output.txt");
        var selection = await TestProcess.Run("dotnet", _repositoryRoot,
            ["ci", "select", "--base", baseline, "--root", RootPath, "--selection", Path.Combine(RootPath, "selection"), "--github-output", outputs], ct);
        selection.ExitCode.ShouldBe(0, $"dotnet ci select: {selection.StandardOutput}{selection.StandardError}");
        return (await File.ReadAllLinesAsync(outputs, ct))
            .Select(static line => line.Split('=', 2))
            .ToDictionary(static pair => pair[0], static pair => pair[1], StringComparer.Ordinal);
    }

    /// <summary>Deletes the temporary fixture repository.</summary>
    public void Dispose() => _directory.Dispose();
}
