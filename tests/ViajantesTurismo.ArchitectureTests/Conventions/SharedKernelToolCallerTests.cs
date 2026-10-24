using PandaBear.Testing;
using static ViajantesTurismo.ArchitectureTests.Conventions.AppHostOrchestrationTestsHelpers;
using static ViajantesTurismo.ArchitectureTests.Conventions.SharedKernelToolCallerTestsHelpers;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

[Trait(TestTraitNames.CategoryName, TestTraitValues.SecurityCategory)]
public sealed class SharedKernelToolCallerTests
{
    [Fact]
    public async Task Every_sharedkernel_tool_command_line_parses_against_the_pinned_tool()
    {
        // Arrange
        var root = GetRepositoryRoot();
        var commandLines = FindCommandLines(root);

        // Act
        List<string> failures = [];
        foreach (var commandLine in commandLines.Where(static line => line.Command != CommandWithoutDiagramDirective))
        {
            if (await ParseWithDiagramDirective(root, commandLine, TestContext.Current.CancellationToken) is { } failure)
            {
                failures.Add(failure);
            }
        }

        // Assert
        string[] knownCallers =
        [
            ".github/actions/setup-ci-prerequisites/action.yml",
            ".github/workflows/dependency-review.yml",
            ".github/workflows/pr-metadata.yml",
            ".github/workflows/roadmap-maintenance.yml",
            "opencode.json",
            "scripts/lint-all.sh",
            "setup-dev.ps1",
            "setup-dev.sh",
        ];
        knownCallers.Except(commandLines.Select(static line => line.Source)).ShouldBeEmpty("The scanner missed known callers.");
        failures.ShouldBeEmpty(string.Join(Environment.NewLine, failures));
    }
}
