using Npgsql;

namespace ViajantesTurismo.HostedTests.Migrations;

/// <summary>Compares EF-owned PostgreSQL schema objects without comparing roles or row contents.</summary>
internal static class MigrationBaselineSchema
{
    private const string ReadShape = """
        WITH relations AS (
            SELECT relation.oid, relation.relkind,
                pg_catalog.format('%I.%I', schema.nspname, relation.relname) AS name
            FROM pg_catalog.pg_class AS relation
            JOIN pg_catalog.pg_namespace AS schema ON schema.oid = relation.relnamespace
            WHERE relation.relkind IN ('r', 'p')
              AND schema.nspname NOT IN ('information_schema', 'migration_fixture', 'branding_fixture')
              AND schema.nspname !~ '^(pg_|governed_migration)'
        ),
        definitions(kind, relation, definition) AS (
            SELECT 'relation', name, relkind::text FROM relations
            UNION ALL
            SELECT 'column', relation.name, pg_catalog.json_build_array(
                column_definition.attnum, column_definition.attname,
                pg_catalog.format_type(column_definition.atttypid, column_definition.atttypmod),
                column_definition.attnotnull, column_definition.attidentity, column_definition.attgenerated,
                pg_catalog.pg_get_expr(default_value.adbin, default_value.adrelid),
                column_collation.collname)::text
            FROM relations AS relation
            JOIN pg_catalog.pg_attribute AS column_definition ON column_definition.attrelid = relation.oid
            LEFT JOIN pg_catalog.pg_attrdef AS default_value
                ON default_value.adrelid = relation.oid AND default_value.adnum = column_definition.attnum
            LEFT JOIN pg_catalog.pg_collation AS column_collation ON column_collation.oid = column_definition.attcollation
            WHERE column_definition.attnum > 0 AND NOT column_definition.attisdropped
            UNION ALL
            SELECT 'constraint', relation.name, pg_catalog.json_build_array(
                constraint_definition.conname, constraint_definition.convalidated,
                pg_catalog.pg_get_constraintdef(constraint_definition.oid))::text
            FROM relations AS relation
            JOIN pg_catalog.pg_constraint AS constraint_definition ON constraint_definition.conrelid = relation.oid
            UNION ALL
            SELECT 'index', relation.name, pg_catalog.json_build_array(
                index_definition.indisvalid, pg_catalog.pg_get_indexdef(index_definition.indexrelid))::text
            FROM relations AS relation
            JOIN pg_catalog.pg_index AS index_definition ON index_definition.indrelid = relation.oid
            UNION ALL
            SELECT 'trigger', relation.name, pg_catalog.json_build_array(
                trigger_definition.tgenabled, pg_catalog.pg_get_triggerdef(trigger_definition.oid),
                pg_catalog.pg_get_functiondef(trigger_definition.tgfoid))::text
            FROM relations AS relation
            JOIN pg_catalog.pg_trigger AS trigger_definition ON trigger_definition.tgrelid = relation.oid
            WHERE NOT trigger_definition.tgisinternal
        )
        SELECT pg_catalog.row_to_json(definitions)::text
        FROM definitions ORDER BY 1;
        """;

    /// <summary>Returns an ordered set of EF-owned relation definitions.</summary>
    internal static async Task<string[]> Read(string connectionString, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand(ReadShape, connection);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var result = new List<string>();
        while (await reader.ReadAsync(ct))
        {
            result.Add(reader.GetString(0));
        }

        return [.. result];
    }
}
