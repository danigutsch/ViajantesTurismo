using System.Text;
using System.Xml.Linq;
using PandaBear.Testing;

namespace ViajantesTurismo.ArchitectureTests.Repository;

[Trait(TestTraitNames.CategoryName, TestTraits.CommandLineCategory)]
public sealed class ComplexityLimitTests
{
    [Fact]
    public async Task Repository_sdk_analysis_rejects_a_method_above_the_complexity_threshold()
    {
        // Arrange
        using var workspace = new TemporaryWorkspace();
        var root = RepositoryTestProcess.GetRepositoryRoot();
        var source = new StringBuilder("internal static class Probe { internal static int Score(int value) { var score = 0;");
        for (var branch = 0; branch < 60; branch++)
        {
            source.Append(System.Globalization.CultureInfo.InvariantCulture, $" if (value == {branch}) {{ score++; }}");
        }

        source.Append(" return score; } }");
        var project = new XElement("Project", new XAttribute("Sdk", "Microsoft.NET.Sdk"),
            new XElement("PropertyGroup", new XElement("IsRoslynComponent", "true")),
            new XElement("Import", new XAttribute("Project", Path.Combine(root, "Directory.Build.props"))));
        workspace.WriteFile("ComplexityProbe.csproj", project.ToString());
        workspace.WriteFile("Probe.cs", source.ToString());
        workspace.WriteFile(".editorconfig", await File.ReadAllTextAsync(Path.Combine(root, ".editorconfig"), TestContext.Current.CancellationToken));

        // Act
        var result = await RepositoryTestProcess.Run("dotnet", root,
            ["build", Path.Combine(workspace.RootPath, "ComplexityProbe.csproj"), "-c", "Release", "--nologo"],
            TestContext.Current.CancellationToken);

        // Assert
        result.ExitCode.ShouldNotBe(0);
        var diagnostics = result.StandardOutput + result.StandardError;
        diagnostics.ShouldContain("error CA1502", StringComparison.Ordinal);
    }
}
