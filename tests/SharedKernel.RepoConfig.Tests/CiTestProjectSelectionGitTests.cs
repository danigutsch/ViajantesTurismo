namespace SharedKernel.RepoConfig.Tests;

[Trait(TestTraitNames.CategoryName, TestTraits.CommandLineCategory)]
public sealed class CiTestProjectSelectionGitTests
{
    [Theory]
    [InlineData("src/SharedKernel/SharedKernel.AI/Feature.cs", "docs/Feature.md")]
    [InlineData("docs/Feature.md", "src/SharedKernel/SharedKernel.AI/Feature.cs")]
    public async Task Renames_between_code_and_docs_select_the_affected_test_project(
        string sourcePath,
        string destinationPath)
    {
        // Arrange
        using var workspace = new TemporaryCiTestRepository();
        var cancellationToken = TestContext.Current.CancellationToken;
        workspace.AddProject("src/SharedKernel/SharedKernel.AI/SharedKernel.AI.csproj");
        workspace.AddProjectWithWindowsReferences(
            "tests/SharedKernel.AI.Tests/SharedKernel.AI.Tests.csproj",
            "src/SharedKernel/SharedKernel.AI/SharedKernel.AI.csproj");
        workspace.WriteSlice("fast-validation", "tests/SharedKernel.AI.Tests/SharedKernel.AI.Tests.csproj");
        await CiTestSelectionTestProcess.Run("git", workspace.RootPath, ["init", "--initial-branch=main"], cancellationToken);
        await CiTestSelectionTestProcess.Run("git", workspace.RootPath, ["config", "user.name", "Repo Config Tests"], cancellationToken);
        await CiTestSelectionTestProcess.Run("git", workspace.RootPath, ["config", "user.email", "repo-config-tests@example.invalid"], cancellationToken);
        await CiTestSelectionTestProcess.Run("git", workspace.RootPath, ["config", "commit.gpgsign", "false"], cancellationToken);
        workspace.WriteFile(sourcePath, "rename regression content");
        await CiTestSelectionTestProcess.Run("git", workspace.RootPath, ["add", "--all"], cancellationToken);
        await CiTestSelectionTestProcess.Run("git", workspace.RootPath, ["commit", "--message", "base"], cancellationToken);
        var baseSha = (await CiTestSelectionTestProcess.Run("git", workspace.RootPath, ["rev-parse", "HEAD"], cancellationToken)).StandardOutput.Trim();

        workspace.MoveFile(sourcePath, destinationPath);
        await CiTestSelectionTestProcess.Run("git", workspace.RootPath, ["add", "--all"], cancellationToken);
        await CiTestSelectionTestProcess.Run("git", workspace.RootPath, ["commit", "--message", "rename"], cancellationToken);
        var headSha = (await CiTestSelectionTestProcess.Run("git", workspace.RootPath, ["rev-parse", "HEAD"], cancellationToken)).StandardOutput.Trim();

        // Act
        var changedPaths = await CiChangedPathReader.Read(
            workspace.RootPath,
            baseSha,
            headSha,
            useMergeBase: true,
            ct: cancellationToken);
        var selection = CiTestProjectSelector.Select(workspace.RootPath, changedPaths, fullValidation: false);

        // Assert
        selection.BuildRequired.ShouldBeTrue();
        selection.FallbackToFullValidation.ShouldBeFalse();
        selection.SelectedProjectsBySlice["fast-validation"]
            .ShouldHaveSingleItem()
            .ShouldBe("tests/SharedKernel.AI.Tests/SharedKernel.AI.Tests.csproj");
    }
}
