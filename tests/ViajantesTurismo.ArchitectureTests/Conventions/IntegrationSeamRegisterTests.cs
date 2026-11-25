using SharedKernel.Testing;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

public sealed class IntegrationSeamRegisterTests
{
    private const string ValidRow =
        "| `WebTests` | `Web` | `Integration` | `test-server` | [HTTP test](WebTests/WebEndpointTests.cs) |";

    [Fact]
    public void Every_registered_integration_and_system_seam_documents_its_tested_entrypoint()
    {
        // Arrange
        var repositoryRoot = RepositoryRoot.Locate("ViajantesTurismo.slnx");

        // Act
        var violations = IntegrationSeamRegisterPolicy.FindViolations(repositoryRoot);

        // Assert
        violations.ShouldBeEmpty();
    }

    [Fact]
    public void Accepts_the_registered_dependency_mode_and_a_test_in_the_owning_suite()
    {
        // Arrange
        using var repository = IntegrationSeamRegisterFixture.Create(ValidRow);

        // Act
        var violations = IntegrationSeamRegisterPolicy.FindViolations(repository.RootPath);

        // Assert
        violations.ShouldBeEmpty();
    }

    [Fact]
    public void Reports_a_missing_register_with_its_path()
    {
        // Arrange
        const string register = "tests/SEAMS.md";
        using var repository = IntegrationSeamRegisterFixture.Create(ValidRow);
        repository.DeleteFile(register);

        // Act
        var violations = IntegrationSeamRegisterPolicy.FindViolations(repository.RootPath);

        // Assert
        violations.ShouldBe([$"Create the test seam register at {register}."]);
    }

    [Theory]
    [Trait(TestTraitNames.CategoryName, TestTraits.DocumentationCategory)]
    [InlineData("public sealed class WebEndpointTests {}")]
    [InlineData("public sealed class WebEndpointTests { public int Status() => 200; }")]
    [InlineData("public sealed class WebEndpointTests { [Fact] public void Reads_the_endpoint() {} }")]
    [InlineData("public abstract class WebEndpointTests { [Xunit.Fact] public abstract void Reads_the_endpoint(); }")]
    public void Reports_links_without_an_implemented_test(string source)
    {
        // Arrange
        using var repository = IntegrationSeamRegisterFixture.Create(ValidRow);
        repository.WriteFile("tests/WebTests/WebEndpointTests.cs", source);

        // Act
        var violations = IntegrationSeamRegisterPolicy.FindViolations(repository.RootPath);

        // Assert
        var violation = violations.ShouldHaveSingleItem();
        violation.ShouldContain("should link the entrypoint", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("", "should document the Integration seam")]
    [InlineData(
        "| `WebTests` | `Web` | `Integration` | `loopback` | [HTTP test](WebTests/WebEndpointTests.cs) |",
        "should record dependency mode 'test-server'")]
    [InlineData(
        "| `WebTests` | `Web` | `Integration` | `test-server` | [HTTP test](WebTests/MissingTests.cs) |",
        "should link the entrypoint")]
    [InlineData(
        "| `WebTests` | `Web` | `Integration` | `test-server` | [HTTP test](OtherTests/OtherEndpointTests.cs) |",
        "should link the entrypoint")]
    [InlineData(ValidRow + "\n" + ValidRow, "undeclared or repeated Integration seam")]
    public void Reports_register_drift_with_a_correction(string rows, string correction)
    {
        // Arrange
        using var repository = IntegrationSeamRegisterFixture.Create(rows);

        // Act
        var violations = IntegrationSeamRegisterPolicy.FindViolations(repository.RootPath);

        // Assert
        var violation = violations.ShouldHaveSingleItem();
        violation.ShouldContain(correction, StringComparison.Ordinal);
    }
}
