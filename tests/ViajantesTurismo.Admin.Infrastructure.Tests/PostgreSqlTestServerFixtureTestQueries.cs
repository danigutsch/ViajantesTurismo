using Npgsql;

namespace ViajantesTurismo.Admin.Infrastructure.Tests;

internal static class PostgreSqlTestServerFixtureTestQueries
{
    public static async Task<string> GetServerIdentity(NpgsqlDataSource dataSource, CancellationToken ct)
    {
        await using var command = dataSource.CreateCommand(
            "SELECT pg_postmaster_start_time()::text || ':' || inet_server_port()::text;");
        var assertionTarget1 = await command.ExecuteScalarAsync(ct);
        return (string)assertionTarget1.ShouldNotBeNull();
    }

    public static async Task CreateOwnedTable(NpgsqlDataSource dataSource, string value, CancellationToken ct)
    {
        await using var command = dataSource.CreateCommand(
            "CREATE TABLE public.fixture_owned_table (value text NOT NULL); "
            + "INSERT INTO public.fixture_owned_table (value) VALUES (@value);");
        command.Parameters.AddWithValue("value", value);
        _ = await command.ExecuteNonQueryAsync(ct);
    }

    public static async Task<string> ReadOwnedValue(NpgsqlDataSource dataSource, CancellationToken ct)
    {
        await using var command = dataSource.CreateCommand("SELECT value FROM public.fixture_owned_table;");
        var assertionTarget2 = await command.ExecuteScalarAsync(ct);
        return (string)assertionTarget2.ShouldNotBeNull();
    }
}
