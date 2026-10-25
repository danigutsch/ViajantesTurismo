using PostgreSqlTestCleanup = ViajantesTurismo.HostedTests.Infrastructure.HostedTestCleanup;
using PandaBear.IntegrationTesting;
using PandaBear.Npgsql.Testing;

namespace ViajantesTurismo.Management.WebIntegrationTests;

internal sealed class SecurityPersistenceApplication(AspireTestApplication application, PostgreSqlTestDatabaseLease database) : IAsyncDisposable
{
    public Task<string> GetConnectionString(string name, CancellationToken ct) => application.GetConnectionString(name, ct);

    public ValueTask DisposeAsync() => new(PostgreSqlTestCleanup.DisposeResources(null, application, database));
}
