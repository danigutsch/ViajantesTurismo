using Npgsql;
using ViajantesTurismo.HostedTests.Admin.Infrastructure.Payments;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Tours;

/// <summary>Reads and writes raw tour currency values so tests can observe the stored column representation.</summary>
internal static class TourCurrencyDatabase
{
    /// <summary>Inserts a tour row whose currency column holds <paramref name="storedCurrency" /> exactly as given.</summary>
    public static async Task InsertTour(PostgreSqlPaymentScenario scenario, string identifier, string storedCurrency, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(scenario.ConnectionString);
        await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO "Tours" ("Id", "Identifier", "Name", "StartDate", "EndDate", "Price", "DoubleRoomSupplementPrice",
                "RegularBikePrice", "EBikePrice", "Currency", "MinCustomers", "MaxCustomers", "IncludedServices")
            VALUES (@id, @identifier, @identifier, DATE '2030-01-01', DATE '2030-01-08', 1000, 10, 10, 10, @currency, 1, 4, ARRAY['Hotel'])
            """,
            connection);
        command.Parameters.AddWithValue("id", Guid.CreateVersion7());
        command.Parameters.AddWithValue("identifier", identifier);
        command.Parameters.AddWithValue("currency", storedCurrency);
        await command.ExecuteNonQueryAsync(ct);
    }

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
