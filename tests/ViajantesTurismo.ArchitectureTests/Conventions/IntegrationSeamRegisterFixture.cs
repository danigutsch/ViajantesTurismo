using ViajantesTurismo.ArchitectureTests.Repository;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>Builds a seam inventory and register for policy tests.</summary>
internal static class IntegrationSeamRegisterFixture
{
    private const string Register = "tests/SEAMS.md";

    /// <summary>Creates one integration seam and the supplied Markdown rows.</summary>
    internal static TemporaryWorkspace Create(string rows)
    {
        var workspace = new TemporaryWorkspace();
        workspace.WriteFile(".config/test-taxonomy.json", $$"""
            {
              "suites": [{
                "path": "tests/WebTests/WebTests.csproj",
                "register": "{{Register}}",
                "seams": [{
                  "sut": "src/Web/Web.csproj",
                  "type": "Integration",
                  "dependency": "test-server"
                }]
              }]
            }
            """);
        workspace.WriteFile(Register, $"""
            ## Integration and system test seams

            | Suite | SUT | Type | Dependency mode | Tested entry point |
            | --- | --- | --- | --- | --- |
            {rows}

            ## Examples for other testing lanes

            | `OtherTests` | `Other` | `Unit` | `in-process` | Example |
            """);
        workspace.WriteFile("tests/WebTests/WebEndpointTests.cs", """
            using System.Net;
            using SharedKernel.Testing.Assertions;
            using EndpointTheory = Xunit.TheoryAttribute;

            public sealed class WebEndpointTests
            {
                [EndpointTheory]
                [Xunit.InlineData(200)]
                public void Reports_the_success_status(int status) => ((int)HttpStatusCode.OK).ShouldBe(status);
            }
            """);
        workspace.WriteFile("tests/OtherTests/OtherEndpointTests.cs", """
            using System.Net;
            using SharedKernel.Testing.Assertions;
            using Xunit;

            public sealed class OtherEndpointTests
            {
                [Fact]
                public void Reports_the_not_found_status()
                {
                    ((int)HttpStatusCode.NotFound).ShouldBe(404);
                }
            }
            """);
        return workspace;
    }
}
