using Npgsql;
using ViajantesTurismo.HostedTests.Admin.Infrastructure.Payments;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Tours;

/// <summary>Reads raw tour currency values so tests can observe the stored column representation.</summary>
internal static class TourCurrencyDatabase
{
    /// <summary>Reads the currency column of the tour with <paramref name="identifier" />.</summary>
    public static async Task<string?> ReadCurrency(PostgreSqlPaymentScenario scenario, string identifier, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(scenario.ConnectionString);
        await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand("""SELECT "Currency" FROM "Tours" WHERE "Identifier" = @identifier""", connection);
        command.Parameters.AddWithValue("identifier", identifier);
        return (string?)await command.ExecuteScalarAsync(ct);
    }

    /// <summary>Reads the declared maximum length of the currency column.</summary>
    public static async Task<int?> ReadCurrencyColumnMaxLength(PostgreSqlPaymentScenario scenario, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(scenario.ConnectionString);
        await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand(
            """SELECT character_maximum_length FROM information_schema.columns WHERE table_name = 'Tours' AND column_name = 'Currency'""",
            connection);
        return await command.ExecuteScalarAsync(ct) as int?;
    }
}
