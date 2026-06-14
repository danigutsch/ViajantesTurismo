using System.Data.Common;

namespace ViajantesTurismo.Admin.Testing.Integration.Helpers;

public static class DatabaseResetHelper
{
    private const string ResetPublicTablesSql = """
                                                DO $$
                                                DECLARE
                                                    tables_to_truncate text;
                                                BEGIN
                                                    SELECT string_agg('"' || tablename || '"', ', ')
                                                    INTO tables_to_truncate
                                                    FROM pg_tables
                                                    WHERE schemaname = 'public'
                                                      AND tablename <> '__EFMigrationsHistory';

                                                    IF tables_to_truncate IS NOT NULL THEN
                                                        EXECUTE 'TRUNCATE TABLE ' || tables_to_truncate || ' RESTART IDENTITY CASCADE';
                                                    END IF;
                                                END $$;
                                                """;

    public static async Task ResetPublicTables(DbConnection connection, CancellationToken ct)
    {
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = ResetPublicTablesSql;

        await command.ExecuteNonQueryAsync(ct);
    }
}
