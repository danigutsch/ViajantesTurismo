using SharedKernel.Testing;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Observability;

[Trait(Api.Infrastructure.TestTraits.ComponentName, Api.Infrastructure.TestTraits.PostgreSqlObservabilityComponent)]
[TestFocus(TestFocus.Security)]
[TestScope(TestScope.Integration)]
public sealed class PostgreSqlMetricPrivacyTests(PostgreSqlTestServerFixture server) : IClassFixture<PostgreSqlTestServerFixture>
{
    [Theory]
    [InlineData("admin", false)]
    [InlineData("admin", true)]
    [InlineData("catalog", false)]
    [InlineData("catalog", true)]
    [InlineData("management-security", false)]
    [InlineData("management-ticket-cache", false)]
    public async Task Registered_pools_export_bounded_dimensions_for_live_database_commands(string poolName, bool initialization)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await server.CreateDatabase(ct);
        // The lease's own pool also publishes observable metrics. Only the host should use this database.
        await database.DataSource.DisposeAsync();
        const string poolNameDimension = "db.client.connection.pool.name";
        const string stateDimension = "db.client.connection.state";
        const string systemDimension = "db.system.name";
        string[] allowedDimensions = [poolNameDimension, stateDimension, systemDimension];

        // Act
        var measurements = await PostgreSqlMetricScenario.Query(database.ConnectionString, poolName, initialization, ct);
        var poolMeasurements = measurements.Where(point => point.Tags.Any(tag => tag.Key == poolNameDimension
            && Equals(tag.Value, poolName))).ToArray();

        // Assert
        poolMeasurements.ShouldContain(static point => point.Instrument == "db.client.connection.count"
            && point.Tags.Any(static tag => tag.Key == stateDimension && Equals(tag.Value, "idle")));
        poolMeasurements.ShouldContain(static point => point.Instrument == "db.client.operation.duration"
            && point.Tags.Any(static tag => tag.Key == systemDimension && Equals(tag.Value, "postgresql")));
        var unexpectedDimensions = poolMeasurements.SelectMany(static point => point.Tags)
            .Select(static tag => tag.Key)
            .Distinct(StringComparer.Ordinal)
            .Except(allowedDimensions, StringComparer.Ordinal);
        unexpectedDimensions.ShouldBeEmpty();
        var exposesPrivateValue = measurements.SelectMany(static point => point.Tags)
            .Any(tag => tag.Value is string value
                && (value.Contains(database.DatabaseName, StringComparison.Ordinal)
                    || value.Contains("metric_private_", StringComparison.Ordinal)));
        exposesPrivateValue.ShouldBeFalse();
    }
}
