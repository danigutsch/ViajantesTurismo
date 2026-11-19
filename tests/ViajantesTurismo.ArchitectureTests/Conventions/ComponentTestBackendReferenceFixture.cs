using System.Xml.Linq;
using ViajantesTurismo.ArchitectureTests.Repository;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>Builds a small component suite inventory and project graph for reference-policy tests.</summary>
internal static class ComponentTestBackendReferenceFixture
{
    /// <summary>Gets the fixture's repository-relative component test project path.</summary>
    internal const string SuiteProject = "tests/ComponentTests/ComponentTests.csproj";

    /// <summary>Gets the fixture's repository-relative support project path.</summary>
    internal const string SupportProject = "tests/Support/Support.csproj";

    /// <summary>Gets the fixture's repository-relative API host project path.</summary>
    internal const string ApiProject = "src/ViajantesTurismo.Admin.Api/ViajantesTurismo.Admin.Api.csproj";

    /// <summary>Gets the fixture's repository-relative AppHost project path.</summary>
    internal const string AppHostProject = "src/ViajantesTurismo.AppHost/ViajantesTurismo.AppHost.csproj";

    /// <summary>Creates a workspace with a component suite and the requested project reference.</summary>
    internal static TemporaryWorkspace Create(string suiteReference, bool registerApi = false)
    {
        var workspace = new TemporaryWorkspace();
        workspace.WriteFile(SuiteProject, ProjectWithReference(suiteReference));
        workspace.WriteFile(SupportProject, ProjectWithReference("../../" + AppHostProject));
        workspace.WriteFile(ApiProject, "<Project />");
        workspace.WriteFile(AppHostProject, "<Project />");

        var apiSeam = registerApi ? $$""", {"type":"Api","sut":"{{ApiProject}}"}""" : string.Empty;
        workspace.WriteFile(".config/test-taxonomy.json", $$"""
            {
              "suites": [{
                "path": "{{SuiteProject}}",
                "seams": [{"type":"Component","sut":"src/Web/Web.csproj"}{{apiSeam}}]
              }]
            }
            """);
        return workspace;
    }

    private static string ProjectWithReference(string reference)
    {
        return new XElement("Project",
            new XElement("ItemGroup",
                new XElement("ProjectReference", new XAttribute("Include", reference)))).ToString();
    }
}
