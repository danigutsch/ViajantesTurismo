using Npgsql;

namespace ViajantesTurismo.Admin.Infrastructure.Tests;

/// <summary>Queries used by fixture tests to identify the server and check database isolation.</summary>
internal static class PostgreSqlTestServerFixtureTestQueries
{
    /// <summary>Returns the server start time and port, identifying the server instance.</summary>
    public static async Task<string> GetServerIdentity(NpgsqlDataSource dataSource, CancellationToken ct)
    {
        await using var command = dataSource.CreateCommand(
            "SELECT pg_postmaster_start_time()::text || ':' || inet_server_port()::text;");
        var assertionTarget1 = await command.ExecuteScalarAsync(ct);
        return (string)assertionTarget1.ShouldNotBeNull();
    }

    /// <summary>Creates <c>public.fixture_owned_table</c> and inserts <paramref name="value" />.</summary>
    public static async Task CreateOwnedTable(NpgsqlDataSource dataSource, string value, CancellationToken ct)
    {
        await using var command = dataSource.CreateCommand(
            "CREATE TABLE public.fixture_owned_table (value text NOT NULL); "
            + "INSERT INTO public.fixture_owned_table (value) VALUES (@value);");
        command.Parameters.AddWithValue("value", value);
        _ = await command.ExecuteNonQueryAsync(ct);
    }

    /// <summary>Reads the single value stored in <c>public.fixture_owned_table</c>.</summary>
    public static async Task<string> ReadOwnedValue(NpgsqlDataSource dataSource, CancellationToken ct)
    {
        await using var command = dataSource.CreateCommand("SELECT value FROM public.fixture_owned_table;");
        var assertionTarget2 = await command.ExecuteScalarAsync(ct);
        return (string)assertionTarget2.ShouldNotBeNull();
    }
}
