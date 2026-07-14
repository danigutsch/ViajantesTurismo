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

    [Fact]
    public void PostgreSql_test_database_bounds_disposal()
    {
        // Arrange
        var testDatabasePath = Path.Combine(
            GetRepositoryRoot(),
            "tests",
            "ViajantesTurismo.Admin.IntegrationTests",
            "Infrastructure",
            "PostgreSqlTestDatabase.cs");
        var testDatabaseText = File.ReadAllText(testDatabasePath);

        // Act
        var usesBoundedDisposal = testDatabaseText.Contains(
                "new CancellationTokenSource(DefaultDisposalTimeout)",
                StringComparison.Ordinal)
            && testDatabaseText.Contains("OpenConnectionAsync(timeoutCts.Token)", StringComparison.Ordinal)
            && testDatabaseText.Contains("ExecuteNonQueryAsync(timeoutCts.Token)", StringComparison.Ordinal);

        // Assert
        usesBoundedDisposal.ShouldBeTrue();
    }
}
