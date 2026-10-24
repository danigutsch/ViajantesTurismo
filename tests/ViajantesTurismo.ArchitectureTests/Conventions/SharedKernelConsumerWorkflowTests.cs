using System.Text.Json;
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
        lintJob.ShouldNotContain("restore-targets", StringComparison.Ordinal);
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

    [Fact]
    public void SharedKernel_actions_are_pinned_to_the_candidate_library_revision()
    {
        // Arrange
        var root = GetRepositoryRoot();
        using var pin = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, ".config", "sharedkernel-candidate.json")));
        var revision = pin.RootElement.GetProperty("libraryRevision").GetString();
        var files = Directory.EnumerateFiles(Path.Combine(root, ".github", "workflows"), "*.yml")
            .Concat(Directory.EnumerateFiles(Path.Combine(root, ".github", "actions"), "action.yml", SearchOption.AllDirectories));

        // Act
        var references = files
            .SelectMany(File.ReadLines)
            .Select(static line => line.Trim())
            .Where(static line => line.StartsWith("uses: danigutsch/PandaBear.SharedKernel/", StringComparison.OrdinalIgnoreCase)
                || line.StartsWith("- uses: danigutsch/PandaBear.SharedKernel/", StringComparison.OrdinalIgnoreCase))
            .Select(static line => line.Split('@')[1].Split(' ')[0])
            .ToArray();

        // Assert
        references.ShouldNotBeEmpty();
        references.Where(reference => reference != revision).ShouldBeEmpty($"SharedKernel actions must use libraryRevision {revision}.");
    }
}
