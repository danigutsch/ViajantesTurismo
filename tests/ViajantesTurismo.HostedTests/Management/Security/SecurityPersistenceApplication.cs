using PostgreSqlTestCleanup = ViajantesTurismo.HostedTests.Infrastructure.HostedTestCleanup;
using PandaBear.IntegrationTesting;
using PandaBear.Npgsql.Testing;

namespace ViajantesTurismo.Management.WebIntegrationTests;

/// <summary>Aspire test application together with its PostgreSQL database lease, disposed together.</summary>
/// <param name="application">Running Aspire test application.</param>
/// <param name="database">Database lease owned by the application.</param>
internal sealed class SecurityPersistenceApplication(AspireTestApplication application, PostgreSqlTestDatabaseLease database) : IAsyncDisposable
{
    /// <summary>Returns the connection string of the named resource.</summary>
    public Task<string> GetConnectionString(string name, CancellationToken ct) => application.GetConnectionString(name, ct);

    /// <summary>Disposes the application and database lease.</summary>
    public ValueTask DisposeAsync() => new(PostgreSqlTestCleanup.DisposeResources(null, application, database));
}
