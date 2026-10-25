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
            "ViajantesTurismo.HostedTests", "Admin", "Infrastructure",
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
    public void PostgreSql_test_databases_use_the_shared_pool_with_library_owned_cleanup_guards()
    {
        // Arrange
        var hostedRoot = Path.Combine(GetRepositoryRoot(), "tests", "ViajantesTurismo.HostedTests");
        var project = File.ReadAllText(Path.Combine(hostedRoot, "ViajantesTurismo.HostedTests.csproj"));
        var fixture = File.ReadAllText(Path.Combine(hostedRoot, "Infrastructure", "HostedTestFixture.cs"));

        // Act
        var duplicateDatabaseImplementations = Directory.GetFiles(hostedRoot, "PostgreSqlTestDatabase.cs", SearchOption.AllDirectories);

        // Assert
        project.ShouldContain("PandaBear.SharedKernel.Npgsql.Testing", StringComparison.Ordinal);
        fixture.ShouldContain("PostgreSqlTestDatabasePool", StringComparison.Ordinal);
        // The pool takes its server from the on-demand PostgreSQL start, so a run without database tests never starts one.
        fixture.ShouldContain("new PostgreSqlTestDatabasePool(ct => postgres.Get(ct))", StringComparison.Ordinal);
        duplicateDatabaseImplementations.ShouldBeEmpty();
    }
}
