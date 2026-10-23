using PandaBear.Testing;
using static ViajantesTurismo.ArchitectureTests.Conventions.AppHostOrchestrationTestsHelpers;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

[Trait(TestTraitNames.CategoryName, TestTraitValues.SecurityCategory)]
public sealed class SharedKernelConsumerWorkflowTests
{
    [Fact]
    public void Lint_stages_the_pinned_candidate_and_restores_tools_before_running_linters()
    {
        // Arrange
        var path = Path.Combine(GetRepositoryRoot(), ".github", "workflows", "ci.yml");
        var workflow = File.ReadAllText(path);

        // Act
        var lintJob = workflow.Split("  lint:", StringSplitOptions.None)[1];
        var bootstrapPosition = lintJob.IndexOf("uses: ./.github/actions/setup-ci-prerequisites", StringComparison.Ordinal);
        var lintPosition = lintJob.IndexOf("run: bash scripts/lint-all.sh --skip-markdown", StringComparison.Ordinal);

        // Assert
        bootstrapPosition.ShouldBeGreaterThanOrEqualTo(0);
        lintPosition.ShouldBeGreaterThan(bootstrapPosition);
        lintJob.ShouldContain("restore-targets: tests/ViajantesTurismo.RepoConfig.Tests/ViajantesTurismo.RepoConfig.Tests.csproj", StringComparison.Ordinal);
        lintJob.ShouldContain("trust-dev-certificate: \"false\"", StringComparison.Ordinal);
        lintJob.ShouldNotContain("restore-local-tools: \"false\"", StringComparison.Ordinal);
    }

    [Fact]
    public void Application_ci_does_not_repeat_library_source_API_validation()
    {
        // Arrange
        var path = Path.Combine(GetRepositoryRoot(), ".github", "workflows", "ci.yml");

        // Act
        var workflow = File.ReadAllText(path);

        // Assert
        workflow.ShouldNotContain("check-public-api-baselines", StringComparison.Ordinal);
        workflow.ShouldNotContain("  api-compatibility:", StringComparison.Ordinal);
    }
}
