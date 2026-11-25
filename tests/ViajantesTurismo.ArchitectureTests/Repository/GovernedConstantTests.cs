using System.Xml.Linq;
using SharedKernel.Testing.Roslyn;

namespace ViajantesTurismo.ArchitectureTests.Repository;

/// <summary>Checks ownership of application-governed C# constants across the production build.</summary>
public sealed class GovernedConstantTests
{
    // Selected test builds can omit some production projects. The Solution Build job runs this
    // check explicitly once all production configurations and references have been recorded.
    [Fact(Explicit = true)]
    public void Catalogued_constants_have_one_owner_and_no_unapproved_aliases()
    {
        // Arrange
        var root = RepositoryRoot.Locate("ViajantesTurismo.slnx");
        var solution = XDocument.Load(Path.Combine(root, "ViajantesTurismo.slnx"));
        var projects = solution.Descendants("Project")
            .Select(project => project.Attribute("Path").ShouldNotBeNull().Value.Replace('\\', '/'))
            .Where(path => path.StartsWith("src/", StringComparison.Ordinal) && path.EndsWith(".csproj", StringComparison.Ordinal));
        var manifests = new List<string>();
        foreach (var project in projects)
        {
            var directory = Path.Combine(root, "TestResults", "governed-literals", "Release", Path.GetFileNameWithoutExtension(project));
            Directory.Exists(directory).ShouldBeTrue($"Build ViajantesTurismo.slnx in Release before running the constant guard. Missing {directory}.");
            manifests.Add(Directory.EnumerateFiles(directory, "*.inputs").ShouldHaveSingleItem());
        }

        var catalog = File.ReadAllText(Path.Combine(root, ".config", "GovernedLiterals.txt"));

        // Act
        var errors = GovernedConstantGuard.Validate(manifests.Select(GovernedLiteralManifest.Compile), catalog);

        // Assert
        errors.ShouldBeEmpty(string.Join(Environment.NewLine, errors));
    }
}
