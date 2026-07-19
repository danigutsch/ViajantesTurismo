namespace SharedKernel.RepoConfig.Tests;

[Trait(TestTraitNames.CategoryName, TestTraits.CommandLineCategory)]
public sealed class DetectChangesScriptTests
{
    [Theory]
    [InlineData("src/SharedKernel/SharedKernel.AI/Feature.cs", "docs/Feature.md")]
    [InlineData("docs/Feature.md", "src/SharedKernel/SharedKernel.AI/Feature.cs")]
    public async Task Renames_between_code_and_docs_require_build_and_fast_validation(
        string sourcePath,
        string destinationPath)
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        var cancellationToken = TestContext.Current.CancellationToken;
        await DetectChangesScriptTestProcess.Run("git", workspace.RootPath, ["init", "--initial-branch=main"], cancellationToken);
        await DetectChangesScriptTestProcess.Run("git", workspace.RootPath, ["config", "user.name", "Repo Config Tests"], cancellationToken);
        await DetectChangesScriptTestProcess.Run("git", workspace.RootPath, ["config", "user.email", "repo-config-tests@example.invalid"], cancellationToken);
        await DetectChangesScriptTestProcess.Run("git", workspace.RootPath, ["config", "commit.gpgsign", "false"], cancellationToken);
        workspace.WriteFile(sourcePath, "rename regression content");
        await DetectChangesScriptTestProcess.Run("git", workspace.RootPath, ["add", "--all"], cancellationToken);
        await DetectChangesScriptTestProcess.Run("git", workspace.RootPath, ["commit", "--message", "base"], cancellationToken);
        var baseSha = (await DetectChangesScriptTestProcess.Run("git", workspace.RootPath, ["rev-parse", "HEAD"], cancellationToken)).StandardOutput.Trim();

        var source = Path.Combine(workspace.RootPath, sourcePath);
        var destination = Path.Combine(workspace.RootPath, destinationPath);
        Directory.CreateDirectory(Path.GetDirectoryName(destination) ?? workspace.RootPath);
        File.Move(source, destination);
        await DetectChangesScriptTestProcess.Run("git", workspace.RootPath, ["add", "--all"], cancellationToken);
        await DetectChangesScriptTestProcess.Run("git", workspace.RootPath, ["commit", "--message", "rename"], cancellationToken);
        var headSha = (await DetectChangesScriptTestProcess.Run("git", workspace.RootPath, ["rev-parse", "HEAD"], cancellationToken)).StandardOutput.Trim();
        var outputPath = Path.Combine(workspace.RootPath, "github-output.txt");

        // Act
        var result = await DetectChangesScriptTestProcess.Run(
            "bash",
            workspace.RootPath,
            [Path.Combine(DetectChangesScriptTestProcess.GetRepositoryRoot(), "scripts", "detect-changes.sh")],
            cancellationToken,
            new Dictionary<string, string?>
            {
                ["GITHUB_EVENT_NAME"] = "pull_request",
                ["GITHUB_BASE_SHA"] = baseSha,
                ["GITHUB_HEAD_SHA"] = headSha,
                ["GITHUB_OUTPUT"] = outputPath
            });

        // Assert
        result.ExitCode.ShouldBe(0);
        result.StandardError.ShouldBeEmpty();
        var outputs = await File.ReadAllLinesAsync(outputPath, cancellationToken);
        outputs.ShouldContain("build_required=true");
        outputs.ShouldContain("fast_validation_required=true");
    }
}
