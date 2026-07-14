using static ViajantesTurismo.ArchitectureTests.Conventions.AppHostOrchestrationTestsHelpers;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

public sealed class PostgreSqlObservabilityTestSafetyTests
{
    [Fact]
    public void Collector_scenario_bounds_and_aggregates_role_teardown()
    {
        // Arrange
        var scenarioPath = Path.Combine(
            GetRepositoryRoot(),
            "tests",
            "ViajantesTurismo.Admin.IntegrationTests",
            "Observability",
            "PostgreSqlIndexHealthCollectorScenario.cs");
        var scenarioText = File.ReadAllText(scenarioPath);

        // Act
        var usesBoundedRoleTeardown = scenarioText.Contains("ResourceTeardownTimeout", StringComparison.Ordinal)
            && scenarioText.Contains("CaptureTeardownFailure", StringComparison.Ordinal)
            && scenarioText.Contains("new AggregateException(", StringComparison.Ordinal)
            && scenarioText.Contains("DROP ROLE IF EXISTS", StringComparison.Ordinal);

        // Assert
        usesBoundedRoleTeardown.ShouldBeTrue();
    }
}
