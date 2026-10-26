using PandaBear.Testing;

namespace ViajantesTurismo.ArchitectureTests.Repository;

[Trait(TestTraitNames.CategoryName, TestTraits.CommandLineCategory)]
public sealed class CiRestoreWarningTests
{
    [Fact]
    public void Solution_build_fails_on_blocking_restore_advisories_and_always_uploads_its_warning_reports()
    {
        // Arrange
        var root = RepositoryTestProcess.GetRepositoryRoot();
        var workflow = File.ReadAllText(Path.Combine(root, ".github/workflows/ci.yml"));

        // Act
        var solutionBuild = workflow.Split("  solution-build:", StringSplitOptions.None)[1]
            .Split("\n  quality-gate:", StringSplitOptions.None)[0];
        var restore = solutionBuild.IndexOf("dotnet restore ViajantesTurismo.slnx --locked-mode", StringComparison.Ordinal);
        var verify = solutionBuild.IndexOf("verify-restore-warnings --log", StringComparison.Ordinal);
        var summary = solutionBuild.IndexOf("warning-report --tool Build", StringComparison.Ordinal);
        var build = solutionBuild.IndexOf("dotnet build ViajantesTurismo.slnx", StringComparison.Ordinal);

        // Assert
        restore.ShouldBeGreaterThan(-1);
        solutionBuild.ShouldContain("tee ", StringComparison.Ordinal);
        solutionBuild.ShouldContain("shell: bash", StringComparison.Ordinal);
        verify.ShouldBeGreaterThan(restore);
        summary.ShouldBeGreaterThan(verify);
        summary.ShouldBeGreaterThan(build);
        solutionBuild.ShouldContain("--exceptions .github/ci-warning-exceptions.json", StringComparison.Ordinal);
        solutionBuild.ShouldContain("name: warning-report-solution-build", StringComparison.Ordinal);
        solutionBuild[verify..summary].ShouldNotContain("continue-on-error", StringComparison.Ordinal);
        solutionBuild[build..summary].ShouldNotContain("continue-on-error", StringComparison.Ordinal);
        solutionBuild.ShouldContain("!cancelled()", StringComparison.Ordinal);
    }

    [Fact]
    public void Every_required_job_uploads_a_warning_report_and_one_job_publishes_the_summary()
    {
        // Arrange
        var root = RepositoryTestProcess.GetRepositoryRoot();
        var workflow = File.ReadAllText(Path.Combine(root, ".github/workflows/ci.yml"));

        // Act
        var testSlices = workflow.Split("  test-slices:", StringSplitOptions.None)[1].Split("\n  build-and-test:", StringSplitOptions.None)[0];
        var lint = workflow.Split("\n  lint:", StringSplitOptions.None)[1].Split("\n  warning-summary:", StringSplitOptions.None)[0];
        var summary = workflow.Split("\n  warning-summary:", StringSplitOptions.None)[1];

        // Assert
        testSlices.ShouldContain("warning-report --tool \"Tests $SLICE\"", StringComparison.Ordinal);
        testSlices.ShouldContain("name: warning-report-tests-${{ matrix.id }}", StringComparison.Ordinal);
        lint.ShouldContain("warning-report --tool Lint", StringComparison.Ordinal);
        lint.ShouldContain("name: warning-report-lint", StringComparison.Ordinal);
        summary.ShouldContain("pattern: warning-report-*", StringComparison.Ordinal);
        summary.ShouldContain("warning-summary", StringComparison.Ordinal);
        summary.ShouldContain("!cancelled()", StringComparison.Ordinal);
        summary.ShouldNotContain("continue-on-error", StringComparison.Ordinal);
    }
}
