using PandaBear.Testing;

namespace ViajantesTurismo.ArchitectureTests.Repository;

[Trait(TestTraitNames.CategoryName, TestTraits.CommandLineCategory)]
public sealed class CiRestoreWarningTests
{
    [Fact]
    public void Solution_build_fails_on_blocking_restore_advisories_and_always_publishes_the_warning_summary()
    {
        // Arrange
        var root = RepositoryTestProcess.GetRepositoryRoot();
        var workflow = File.ReadAllText(Path.Combine(root, ".github/workflows/ci.yml"));

        // Act
        var solutionBuild = workflow.Split("  solution-build:", StringSplitOptions.None)[1]
            .Split("\n  quality-gate:", StringSplitOptions.None)[0];
        var restore = solutionBuild.IndexOf("dotnet restore ViajantesTurismo.slnx --locked-mode", StringComparison.Ordinal);
        var verify = solutionBuild.IndexOf("verify-restore-warnings --log", StringComparison.Ordinal);
        var summary = solutionBuild.IndexOf("warning-summary --report", StringComparison.Ordinal);
        var build = solutionBuild.IndexOf("dotnet build ViajantesTurismo.slnx", StringComparison.Ordinal);

        // Assert
        restore.ShouldBeGreaterThan(-1);
        solutionBuild.ShouldContain("tee ", StringComparison.Ordinal);
        solutionBuild.ShouldContain("shell: bash", StringComparison.Ordinal);
        verify.ShouldBeGreaterThan(restore);
        summary.ShouldBeGreaterThan(verify);
        build.ShouldBeGreaterThan(summary);
        solutionBuild.ShouldContain("--summary \"$GITHUB_STEP_SUMMARY\"", StringComparison.Ordinal);
        solutionBuild[verify..summary].ShouldNotContain("continue-on-error", StringComparison.Ordinal);
        solutionBuild[summary..build].ShouldNotContain("continue-on-error", StringComparison.Ordinal);
        solutionBuild.ShouldContain("!cancelled()", StringComparison.Ordinal);
    }
}
