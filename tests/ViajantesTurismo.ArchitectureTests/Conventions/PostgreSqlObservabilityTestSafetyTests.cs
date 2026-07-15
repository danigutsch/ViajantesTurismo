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

    [Fact]
    public void PostgreSql_index_health_hosted_service_delays_after_each_completed_collection_cycle()
    {
        // Arrange
        var hostedServicePath = Path.Combine(
            GetRepositoryRoot(),
            "src",
            "SharedKernel",
            "SharedKernel.Observability.Npgsql",
            "PostgreSqlIndexHealthHostedService.cs");
        var hostedServiceText = File.ReadAllText(hostedServicePath);

        // Act
        var waitsAfterCompletedCollection = hostedServiceText.Contains(
            """
                            foreach (var collector in collectors)
                            {
                                _ = await collector.Collect(stoppingToken).ConfigureAwait(false);
                            }

                            using var timer = new PeriodicTimer(registration.Options.PollingInterval);
                            _ = await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false);
            """,
            StringComparison.Ordinal);

        // Assert
        waitsAfterCompletedCollection.ShouldBeTrue();
    }
}
