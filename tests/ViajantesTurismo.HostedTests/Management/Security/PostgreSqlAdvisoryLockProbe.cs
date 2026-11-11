using Npgsql;
using PandaBear.IntegrationTesting;

namespace ViajantesTurismo.HostedTests.Management.Security;

/// <summary>Observes PostgreSQL advisory locks of the current database.</summary>
internal static class PostgreSqlAdvisoryLockProbe
{
    /// <summary>Waits until a session of the current database waits for an advisory lock another session holds.</summary>
    public static async Task WaitForBlockedLock(NpgsqlDataSource dataSource, TimeSpan timeout, CancellationToken ct)
    {
        _ = await Eventually.Until(
            async ct =>
            {
                await using var command = dataSource.CreateCommand(
                    """
                    SELECT EXISTS (
                        SELECT 1 FROM pg_locks
                        WHERE locktype = 'advisory'
                          AND NOT granted
                          AND database = (SELECT oid FROM pg_database WHERE datname = current_database()));
                    """);
                return await command.ExecuteScalarAsync(ct) is true ? "blocked" : null;
            },
            timeout,
            ct);
    }
}
