namespace SharedKernel.RepoConfig.Tests;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, "coverage")]
public sealed class CoverageGateTests
{
    [Theory]
    [InlineData(4, 0)]
    [InlineData(5, 0)]
    [InlineData(3, 1)]
    public async Task Changed_code_requires_eighty_percent_coverage(int coveredLines, int expectedExit)
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        CoverageGateTestFiles.Write(workspace, coveredLines);
        using var output = new StringWriter();
        using var error = new StringWriter();

        // Act
        var exit = await RepoConfigToolApplication.Run(
            ["verify-coverage", "--report", "coverage.xml", "--diff", "changes.patch"],
            output, error, workspace.RootPath, TestContext.Current.CancellationToken);

        // Assert
        exit.ShouldBe(expectedExit);
        output.ToString().ShouldContain("changed executable", StringComparison.Ordinal);
        workspace.FileExists("TestResults/coverage-gate.json").ShouldBeTrue();
    }

    [Fact]
    public async Task Uncovered_conditions_count_towards_the_changed_code_threshold()
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        CoverageGateTestFiles.Write(workspace, 4, conditions: "50% (1/2)");
        using var output = new StringWriter();
        using var error = new StringWriter();

        // Act
        var exit = await RepoConfigToolApplication.Run(
            ["verify-coverage", "--report", "coverage.xml", "--diff", "changes.patch"],
            output, error, workspace.RootPath, TestContext.Current.CancellationToken);

        // Assert
        exit.ShouldBe(1);
        output.ToString().ShouldContain("5/7", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("malformed")]
    [InlineData("omitted-source")]
    public async Task Missing_or_invalid_coverage_evidence_fails_closed(string problem)
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        CoverageGateTestFiles.Write(workspace, 5);
        if (problem == "missing") { workspace.DeleteFile("coverage.xml"); }
        if (problem == "malformed") { workspace.WriteFile("coverage.xml", "<coverage>"); }
        if (problem == "omitted-source")
        {
            workspace.WriteFile("coverage.xml", "<coverage><packages /></coverage>");
        }
        using var output = new StringWriter();
        using var error = new StringWriter();

        // Act
        var exit = await RepoConfigToolApplication.Run(
            ["verify-coverage", "--report", "coverage.xml", "--diff", "changes.patch"],
            output, error, workspace.RootPath, TestContext.Current.CancellationToken);

        // Assert
        exit.ShouldBe(1);
        error.ToString().ShouldNotBeEmpty();
    }

    [Theory]
    [InlineData("duplicate", 1)]
    [InlineData("rename", 0)]
    [InlineData("delete", 0)]
    [InlineData("non-executable", 0)]
    [InlineData("tests-only", 0)]
    [InlineData("malformed-hunk", 1)]
    [InlineData("truncated-hunk", 1)]
    [InlineData("invalid-start", 1)]
    [InlineData("header-without-hunk", 1)]
    [InlineData("missing-diff", 1)]
    [InlineData("dtd", 1)]
    [InlineData("internal-entity", 1)]
    [InlineData("reportgenerator-doctype", 0)]
    public async Task Coverage_evidence_handles_diff_boundaries_without_inflating_coverage(string scenario, int expectedExit)
    {
        // Arrange
        using var workspace = new TemporaryRepoConfigWorkspace();
        CoverageGateTestFiles.Write(workspace, 3);
        CoverageGateTestFiles.ApplyScenario(workspace, scenario);
        using var output = new StringWriter();
        using var error = new StringWriter();

        // Act
        var exit = await RepoConfigToolApplication.Run(
            ["verify-coverage", "--report", "coverage.xml", "--diff", "changes.patch"],
            output, error, workspace.RootPath, TestContext.Current.CancellationToken);

        // Assert
        exit.ShouldBe(expectedExit);
        workspace.FileExists("TestResults/coverage-gate.json").ShouldBeTrue();
    }
}
