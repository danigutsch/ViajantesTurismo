using Npgsql;
namespace ViajantesTurismo.Management.WebIntegrationTests;

/// <summary>Queries used by fixture tests to check server identity and database ownership.</summary>
internal static class PostgreSqlTestServerFixtureTestQueries
{
    /// <summary>Returns the server start time and port as an identity string.</summary>
    public static async Task<string> GetServerIdentity(NpgsqlDataSource dataSource, CancellationToken ct)
    {
        await using var command = dataSource.CreateCommand(
            "SELECT pg_postmaster_start_time()::text || ':' || inet_server_port()::text;");
        var scalar = await command.ExecuteScalarAsync(ct);
        return (string)scalar.ShouldNotBeNull();
    }

    /// <summary>Creates a table and inserts the given value into it.</summary>
    public static async Task CreateOwnedTable(NpgsqlDataSource dataSource, string value, CancellationToken ct)
    {
        await using var command = dataSource.CreateCommand(
            "CREATE TABLE public.fixture_owned_table (value text NOT NULL); "
            + "INSERT INTO public.fixture_owned_table (value) VALUES (@value);");
        command.Parameters.AddWithValue("value", value);
        _ = await command.ExecuteNonQueryAsync(ct);
    }

    /// <summary>Reads the value inserted by CreateOwnedTable.</summary>
    public static async Task<string> ReadOwnedValue(NpgsqlDataSource dataSource, CancellationToken ct)
    {
        await using var command = dataSource.CreateCommand("SELECT value FROM public.fixture_owned_table;");
        var scalar = await command.ExecuteScalarAsync(ct);
        return (string)scalar.ShouldNotBeNull();
    }
}
