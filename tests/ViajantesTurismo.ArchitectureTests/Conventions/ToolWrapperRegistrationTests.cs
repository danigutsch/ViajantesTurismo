using System.Xml.Linq;
using static ViajantesTurismo.ArchitectureTests.Conventions.AdminTestArchitectureGuardTestsHelpers;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

public sealed class ToolWrapperRegistrationTests
{
    [Fact]
    public void Every_repository_tool_enables_the_thin_wrapper_analyzer()
    {
        // Arrange
        var root = GetRepositoryRoot();
        var projects = Directory.GetFiles(Path.Combine(root, "scripts"), "*.Tool.csproj", SearchOption.AllDirectories);

        // Act
        var diagnostics = projects
            .Where(project => !IsGeneratedTestPath(project))
            .SelectMany(project => ToolWrapperRegistrationPolicy.Inspect(root, project, XDocument.Load(project)))
            .ToArray();

        // Assert
        projects.ShouldNotBeEmpty();
        diagnostics.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("ViajantesTurismoToolWrapper", "opt in")]
    [InlineData("CompilerVisibleProperty", "CompilerVisibleProperty")]
    [InlineData("ProjectReference", "analyzer project reference")]
    [InlineData("OutputItemType", "analyzer project reference")]
    [InlineData("ReferenceOutputAssembly", "analyzer project reference")]
    public void A_missing_registration_element_has_an_actionable_diagnostic(string missingElement, string expectedMessage)
    {
        // Arrange
        var root = GetRepositoryRoot();
        var path = Path.Combine(root, "scripts", "Example.Tool", "Example.Tool.csproj");
        var project = XDocument.Parse("""
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <ViajantesTurismoToolWrapper>true</ViajantesTurismoToolWrapper>
                </PropertyGroup>
                <ItemGroup>
                    <CompilerVisibleProperty Include="ViajantesTurismoToolWrapper" />
                    <ProjectReference Include="../../src/ViajantesTurismo.Architecture.Analyzers/ViajantesTurismo.Architecture.Analyzers.csproj">
                        <OutputItemType>Analyzer</OutputItemType>
                        <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
                    </ProjectReference>
                </ItemGroup>
            </Project>
            """);
        project.Descendants(missingElement).ShouldHaveSingleItem().Remove();

        // Act
        var diagnostics = ToolWrapperRegistrationPolicy.Inspect(root, path, project);

        // Assert
        var diagnostic = diagnostics.ShouldHaveSingleItem();
        diagnostic.ShouldContain("scripts/Example.Tool/Example.Tool.csproj", StringComparison.Ordinal);
        diagnostic.ShouldContain(expectedMessage, StringComparison.Ordinal);
    }
}
