using Npgsql;

namespace ViajantesTurismo.MigrationService.GovernedMigrations;

/// <summary>Executes fixed journal statement templates qualified with validated schema identifiers.</summary>
internal static class MigrationJournalSql
{
    /// <summary>Sends one qualified journal statement and binds its data values separately.</summary>
    internal static NpgsqlBatch Create(NpgsqlConnection connection, NpgsqlTransaction? transaction, string sql, params object[] values)
    {
        var statement = new NpgsqlBatchCommand(sql);
        foreach (var value in values)
        {
            statement.Parameters.AddWithValue(value);
        }

        return new NpgsqlBatch(connection, transaction) { BatchCommands = { statement } };
    }
}
