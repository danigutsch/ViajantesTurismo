using System.Text.Json;
using PandaBear.Testing;
using static ViajantesTurismo.ArchitectureTests.Conventions.AppHostOrchestrationTestsHelpers;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

[Trait(TestTraitNames.CategoryName, TestTraitValues.SecurityCategory)]
public sealed class SharedKernelConsumerWorkflowTests
{
    [Fact]
    public void Lint_stages_the_pinned_candidate_and_restores_tools_before_running_checks()
    {
        // Arrange
        var path = Path.Combine(GetRepositoryRoot(), ".github", "workflows", "ci.yml");
        var workflow = File.ReadAllText(path);

        // Act
        var lintJob = workflow.Split("  lint:", StringSplitOptions.None)[1];
        var bootstrapPosition = lintJob.IndexOf("uses: ./.github/actions/setup-ci-prerequisites", StringComparison.Ordinal);
        var lintPosition = lintJob.IndexOf("dotnet tool run sharedkernel-repo --", StringComparison.Ordinal);

        // Assert
        bootstrapPosition.ShouldBeGreaterThanOrEqualTo(0);
        lintPosition.ShouldBeGreaterThan(bootstrapPosition);
        lintJob.IndexOf("dotnet tool run sharedkernel-docs --", StringComparison.Ordinal).ShouldBeGreaterThan(bootstrapPosition);
        lintJob.ShouldNotContain("bash scripts/", StringComparison.Ordinal);
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
    public void SharedKernel_scanner_action_is_pinned_to_the_candidate_library_revision()
    {
        // Arrange
        var root = GetRepositoryRoot();
        using var pin = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, ".config", "sharedkernel-candidate.json")));
        var revision = pin.RootElement.GetProperty("libraryRevision").GetString();

        // Act
        var references = SharedKernelWorkflowReferences.SharedKernelReferences(root).Where(static reference => reference.Path.Contains("/actions/setup-security-scanner", StringComparison.Ordinal))
            .Select(static reference => reference.Revision)
            .ToArray();

        // Assert
        references.ShouldNotBeEmpty();
        references.Where(reference => reference != revision).ShouldBeEmpty($"SharedKernel scanner actions must use libraryRevision {revision}.");
    }

    [Fact]
    public void Shared_workflow_references_use_one_revision()
    {
        // Arrange
        var root = GetRepositoryRoot();
        var files = SharedKernelWorkflowReferences.WorkflowFiles(root);

        // Act
        var references = SharedKernelWorkflowReferences.SharedKernelReferences(root)
            .Where(static reference => !reference.Path.Contains("/actions/setup-security-scanner", StringComparison.Ordinal))
            .Select(static reference => reference.Revision)
            .Concat(files.SelectMany(File.ReadLines)
                .Select(static line => line.Trim())
                .Where(static line => line.StartsWith("source-ref:", StringComparison.Ordinal) || line.StartsWith("revision:", StringComparison.Ordinal))
                .Select(static line => line[(line.IndexOf(':', StringComparison.Ordinal) + 1)..].Trim())
                .Where(static value => value.Length > 0))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        // Assert
        references.Length.ShouldBe(1, "Shared workflow, shared action and source-ref values must move together in the pin bump.");
        references[0].Length.ShouldBe(40);
    }
}
