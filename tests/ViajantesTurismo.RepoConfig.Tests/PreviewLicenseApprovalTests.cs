namespace ViajantesTurismo.RepoConfig.Tests;

[Trait(TestTraitNames.CategoryName, TestTraits.CommandLineCategory)]
public sealed class PreviewLicenseApprovalTests
{
    [Theory]
    [InlineData("0.1.0-alpha.70.1", "2.25.29", "15.0.0", false, true, false, true)]
    [InlineData("1.0.0-alpha.20261021.2", "2.25.29", "15.0.0", false, true, false, false)]
    [InlineData("1.2.0-alpha.20261023.1", "2.25.29", "15.0.0", false, true, false, false)]
    [InlineData("1.0.0", "2.25.29", "15.0.0", false, true, false, false)]
    [InlineData("1.2.0-alpha.20261024.1", "2.25.29", "15.0.0", false, true, false, false)]
    [InlineData("0.1.0-alpha.70.1", "2.25.30", "15.0.0", false, true, false, false)]
    [InlineData("0.1.0-alpha.70.1", "2.25.29", "15.0.1", false, true, false, false)]
    [InlineData("0.1.0-alpha.70.1", "2.25.29", "15.0.0", true, true, false, false)]
    [InlineData("0.1.0-alpha.70.1", "2.25.29", "15.0.0", false, false, false, false)]
    [InlineData("0.1.0-alpha.70.1", "2.25.29", "15.0.0", false, true, true, false)]
    [InlineData("0.1.0-alpha.70.1", "", "15.0.0", false, true, false, false)]
    [InlineData("0.1.0-alpha.70.1", "2.25.29", "", false, true, false, false)]
    public async Task Preview_exception_requires_the_approved_candidate_and_every_locked_version(
        string candidateVersion, string dependencyVersion, string magickVersion, bool dirty, bool correctRevision,
        bool conflictingTarget, bool approved)
    {
        // Arrange
        using var repository = new TemporaryCiTestRepository();
        var root = CiTestSelectionTestProcess.GetRepositoryRoot();
        var workflow = await File.ReadAllTextAsync(
            Path.Combine(root, ".github", "workflows", "dependency-review.yml"), TestContext.Current.CancellationToken);
        var step = workflow.Split("id: preview-license", StringSplitOptions.None)[1]
            .Split("      - name: Dependency Review", StringSplitOptions.None)[0];
        var script = string.Join('\n', step.Split("run: |", StringSplitOptions.None)[1]
            .Split('\n').Select(line => line.StartsWith("          ", StringComparison.Ordinal) ? line[10..] : line));
        Directory.CreateDirectory(Path.Combine(repository.RootPath, ".config"));
        var pin = System.Text.Json.JsonSerializer.Serialize(new
        {
            packageVersion = candidateVersion,
            libraryRevision = correctRevision ? "ef0dd0a67b8982dfdf0cb8f5e23224c397971a5b" : new string('a', 40),
            libraryDirty = dirty
        });
        await File.WriteAllTextAsync(Path.Combine(repository.RootPath, ".config", "sharedkernel-candidate.json"), pin, TestContext.Current.CancellationToken);
        Directory.CreateDirectory(Path.Combine(repository.RootPath, "src"));
        var packageName = string.IsNullOrEmpty(dependencyVersion) ? "OtherPackage" : "StreamJsonRpc";
        var magickName = string.IsNullOrEmpty(magickVersion) ? "OtherImagePackage" : "Magick.NET-Q8-AnyCPU";
        var graph = System.Text.Json.JsonSerializer.Serialize(new
        {
            dependencies = new Dictionary<string, object>
            {
                ["net10.0"] = new Dictionary<string, object>
                {
                    [packageName] = new { resolved = dependencyVersion },
                    [magickName] = new { resolved = magickVersion }
                },
                ["net10.0/win-x64"] = new Dictionary<string, object>
                {
                    [packageName] = new { resolved = conflictingTarget ? "2.25.30" : dependencyVersion }
                }
            }
        });
        await File.WriteAllTextAsync(Path.Combine(repository.RootPath, "src", "packages.lock.json"), graph, TestContext.Current.CancellationToken);
        await repository.CommitFiles(TestContext.Current.CancellationToken);
        var outputPath = Path.Combine(repository.RootPath, "github-output");

        // Act
        var result = await CiTestSelectionTestProcess.Run("pwsh", repository.RootPath,
            ["-NoProfile", "-NonInteractive", "-Command", script], TestContext.Current.CancellationToken,
            new Dictionary<string, string?> { ["GITHUB_OUTPUT"] = outputPath });

        // Assert
        var succeeded = result.ExitCode == 0;
        succeeded.ShouldBe(approved);
        File.Exists(outputPath).ShouldBe(approved);
        if (approved)
        {
            var output = await File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken);
            output.Trim().ShouldBe("packages=pkg:nuget/StreamJsonRpc@2.25.29,pkg:nuget/Magick.NET-Q8-AnyCPU@15.0.0");
        }
    }
}
