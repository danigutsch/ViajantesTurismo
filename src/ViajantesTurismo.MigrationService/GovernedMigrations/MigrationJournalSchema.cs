using Npgsql;

namespace ViajantesTurismo.MigrationService.GovernedMigrations;

/// <summary>Binds the reviewed context to a stable PostgreSQL journal schema.</summary>
internal sealed class MigrationJournalSchema
{
    private const string DefaultName = "governed_migration";
    private const string DefaultAttempts = DefaultName + ".attempts";
    private const string ScopedPrefix = DefaultName + "_";
    private const int MaximumIdentifierLength = 63;
    private readonly string? scopedName;

    private MigrationJournalSchema(string name, string? scopedName)
    {
        Name = name;
        this.scopedName = scopedName;
    }

    /// <summary>The validated unquoted identifier used for journal SQL.</summary>
    internal string Name
    {
        get;
    }

    /// <summary>The qualified attempt table used by both journals.</summary>
    internal string Attempts => Name + ".attempts";

    /// <summary>Resolves the optional canonical binding without changing the legacy default.</summary>
    internal static MigrationJournalSchema Resolve(MigrationManifest manifest)
    {
        var context = manifest.Context;
        var scoped = context.Length > 0 && context.Length <= MaximumIdentifierLength - ScopedPrefix.Length
            && context.All(static character => character is >= 'a' and <= 'z' or >= '0' and <= '9' or '_')
            ? ScopedPrefix + context
            : null;
        if (manifest.JournalSchema is null)
        {
            return new MigrationJournalSchema(DefaultName, scoped);
        }

        if (scoped is null || !string.Equals(scoped, manifest.JournalSchema, StringComparison.Ordinal))
        {
            throw new InvalidDataException("A journal schema must be governed_migration_ followed by the lowercase context identifier, within PostgreSQL's identifier limit.");
        }

        return new MigrationJournalSchema(scoped, scoped);
    }

    /// <summary>Checks scoped ownership and protects existing history when selecting a journal.</summary>
    internal async Task ValidateProvisioning(NpgsqlConnection owner, NpgsqlTransaction transaction, string context, CancellationToken ct)
    {
        if (Name == DefaultName)
        {
            if (scopedName is null)
            {
                return;
            }

            await using var scopedAttempts = new NpgsqlCommand("""
                SELECT EXISTS (
                    SELECT 1 FROM pg_catalog.pg_class AS relation
                    JOIN pg_catalog.pg_namespace AS journal ON journal.oid = relation.relnamespace
                    WHERE journal.nspname = $1 AND relation.relname = 'attempts' AND relation.relkind IN ('r', 'p')
                );
                """, owner, transaction);
            MigrationPostgreSql.Bind(scopedAttempts, scopedName);
            if (await scopedAttempts.ExecuteScalarAsync(ct) is true)
            {
                throw new InvalidDataException("This context already has a scoped migration journal. Declare its bound journal schema.");
            }

            return;
        }

        await using var owned = new NpgsqlCommand("""
            SELECT EXISTS (
                SELECT 1 FROM pg_catalog.pg_namespace
                WHERE nspname = $1 AND nspowner = (SELECT oid FROM pg_catalog.pg_roles WHERE rolname = current_user)
            );
            """, owner, transaction);
        MigrationPostgreSql.Bind(owned, Name);
        if (await owned.ExecuteScalarAsync(ct) is not true)
        {
            throw new InvalidDataException("The selected journal schema must be provisioned and owned by the migration owner login.");
        }

        await using var legacyVisible = new NpgsqlCommand("""
            SELECT EXISTS (
                SELECT 1 FROM pg_catalog.pg_class AS relation
                JOIN pg_catalog.pg_namespace AS journal ON journal.oid = relation.relnamespace
                WHERE journal.nspname = $1 AND relation.relname = 'attempts' AND relation.relkind IN ('r', 'p')
                  AND pg_catalog.has_schema_privilege(journal.oid, 'USAGE')
                  AND pg_catalog.has_table_privilege(relation.oid, 'SELECT')
            );
            """, owner, transaction);
        MigrationPostgreSql.Bind(legacyVisible, DefaultName);
        if (await legacyVisible.ExecuteScalarAsync(ct) is true)
        {
            await using var legacyHistory = new NpgsqlCommand($"SELECT EXISTS (SELECT 1 FROM {DefaultAttempts} WHERE context = $1);",
                owner, transaction);
            MigrationPostgreSql.Bind(legacyHistory, context);
            if (await legacyHistory.ExecuteScalarAsync(ct) is true)
            {
                throw new InvalidDataException("This context already has a default migration journal. Continue using the original schema.");
            }
        }
    }
}
