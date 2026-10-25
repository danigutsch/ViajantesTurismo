using System.Data.Common;

namespace ViajantesTurismo.Admin.SystemTests.Infrastructure.Helpers;

internal static class PostgreSqlEventSourcingSchemaReset
{
    private const string ResetEventSourcingTablesSql = """
                                                     DO $$
                                                     DECLARE
                                                         tables_to_truncate text;
                                                     BEGIN
                                                         SELECT string_agg(format('%I.%I', schemaname, tablename), ', ')
                                                         INTO tables_to_truncate
                                                         FROM pg_tables
                                                         WHERE schemaname = 'event_sourcing';

                                                         IF tables_to_truncate IS NOT NULL THEN
                                                             EXECUTE 'TRUNCATE TABLE ' || tables_to_truncate || ' RESTART IDENTITY CASCADE';
                                                         END IF;
                                                     END $$;
                                                     """;

    public static async Task Reset(DbConnection connection, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(connection);

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        using var command = connection.CreateCommand();
        command.CommandText = ResetEventSourcingTablesSql;
        await command.ExecuteNonQueryAsync(ct);
    }
}
