using ViajantesTurismo.ArchitectureTests.Repository;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

public sealed class RawTestServiceProviderBoundaryTests
{
    [Theory]
    [InlineData("[Fact] public void Inline() { provider.CreateScope(); }", 4, "provider.CreateScope()")]
    [InlineData("[Fact]\npublic void Short() => services.BuildServiceProvider();", 5, "services.BuildServiceProvider()")]
    [InlineData("[global::Xunit.TheoryAttribute]\npublic void Qualified() {\n    provider.CreateAsyncScope();\n}", 6, "provider.CreateAsyncScope()")]
    [InlineData("[TestCase]\npublic void Alias() {\n    _ = new Microsoft.Extensions.DependencyInjection.ServiceCollection();\n}",
        6, "new Microsoft.Extensions.DependencyInjection.ServiceCollection()")]
    public void Reports_raw_provider_operations_from_concrete_xUnit_methods(string method, int line, string operation)
    {
        // Arrange
        using var workspace = new TemporaryWorkspace();
        const string fileName = "Test.cs";
        workspace.WriteFile(fileName, $$"""
            using TestCase = global::Xunit.FactAttribute;
            public sealed class TestFixture
            {
                {{method}}
            }
            """);
        var path = Path.Combine(workspace.RootPath, fileName);

        // Act
        var violations = AdminTestArchitectureGuardTestsHelpers.FindRawServiceProviderPlumbingInTestMethods(path);

        // Assert
        var violation = violations.ShouldHaveSingleItem();
        violation.ShouldEndWith($"{fileName}:L{line} {operation}", StringComparison.Ordinal);
    }

    [Fact]
    public void Allows_fixture_plumbing_and_ignores_provider_names_in_example_text()
    {
        // Arrange
        using var workspace = new TemporaryWorkspace();
        const string fileName = "Test.cs";
        workspace.WriteFile(fileName, """
            public sealed class TestFixture
            {
                public TestFixture()
                {
                    var services = new ServiceCollection();
                    services.BuildServiceProvider();
                }

                [Fact]
                public void Uses_typed_fixture()
                {
                    // A fixture should own CreateScope().
                    var example = "new ServiceCollection()";
                    var scoped = fixture.CreateScenario();
                }

                private void Configure_fixture()
                {
                    provider.CreateAsyncScope();
                }
            }
            """);
        var path = Path.Combine(workspace.RootPath, fileName);

        // Act
        var violations = AdminTestArchitectureGuardTestsHelpers.FindRawServiceProviderPlumbingInTestMethods(path);

        // Assert
        violations.ShouldBeEmpty();
    }
}
