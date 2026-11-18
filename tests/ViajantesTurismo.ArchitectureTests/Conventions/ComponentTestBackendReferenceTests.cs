using ViajantesTurismo.ArchitectureTests.Dependencies;
using static ViajantesTurismo.ArchitectureTests.Conventions.ComponentTestBackendReferenceFixture;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

public sealed class ComponentTestBackendReferenceTests
{
    [Fact]
    public void Registered_component_suites_only_reference_declared_backend_hosts()
    {
        // Arrange
        var repositoryRoot = LayerDependencyTestsHelpers.GetRepositoryRoot();

        // Act
        var violations = ComponentTestBackendReferencePolicy.FindViolations(repositoryRoot);

        // Assert
        violations.ShouldBeEmpty();
    }

    [Fact]
    public void Reports_an_undeclared_API_project_reference()
    {
        // Arrange
        using var workspace = Create("../../" + ApiProject);

        // Act
        var violations = ComponentTestBackendReferencePolicy.FindViolations(workspace.RootPath);

        // Assert
        var violation = violations.ShouldHaveSingleItem();
        violation.ShouldContain($"{SuiteProject} -> {ApiProject}", StringComparison.Ordinal);
    }

    [Fact]
    public void Reports_an_undeclared_application_host_through_test_support()
    {
        // Arrange
        using var workspace = Create(@"..\Support\Support.csproj");

        // Act
        var violations = ComponentTestBackendReferencePolicy.FindViolations(workspace.RootPath);

        // Assert
        var violation = violations.ShouldHaveSingleItem();
        violation.ShouldContain($"{SuiteProject} -> {SupportProject} -> {AppHostProject}", StringComparison.Ordinal);
    }

    [Fact]
    public void Allows_an_API_host_registered_for_a_separate_seam_in_a_mixed_suite()
    {
        // Arrange
        using var workspace = Create("../../" + ApiProject, registerApi: true);

        // Act
        var violations = ComponentTestBackendReferencePolicy.FindViolations(workspace.RootPath);

        // Assert
        violations.ShouldBeEmpty();
    }
}
