using System.Buffers.Binary;
using System.Data.Common;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Npgsql;

namespace ViajantesTurismo.MigrationService.GovernedMigrations;

/// <summary>Provides the PostgreSQL session and transaction boundaries for governed migrations.</summary>
internal static class MigrationPostgreSql
{
    private const string ExpansionFunction = """
        CREATE FUNCTION pg_temp.execute_governed_migration(script text) RETURNS void
        LANGUAGE plpgsql SECURITY INVOKER AS $governed$
        BEGIN
            EXECUTE script;
        END;
        $governed$;
        """;

    private const string VerificationFunction = """
        CREATE FUNCTION pg_temp.verify_governed_migration(script text) RETURNS SETOF boolean
        LANGUAGE plpgsql SECURITY INVOKER AS $governed$
        BEGIN
            RETURN QUERY EXECUTE script;
        END;
        $governed$;
        """;

    /// <summary>Validates both connection targets before opening either session.</summary>
    internal static (NpgsqlConnectionStringBuilder Owner, NpgsqlConnectionStringBuilder Verifier) ConnectionSettings(
        string? owner, string? verifier)
    {
        var ownerSettings = ParseSettings(owner);
        var verifierSettings = ParseSettings(verifier);
        if (string.Equals(ownerSettings.Username, verifierSettings.Username, StringComparison.Ordinal))
        {
            throw new InvalidDataException("Migration owner and verifier require separate PostgreSQL logins.");
        }

        RequireSameTarget(ownerSettings, verifierSettings);
        return (ownerSettings, verifierSettings);
    }

    /// <summary>Validates the data credential without requiring it for manifests that skip data.</summary>
    internal static NpgsqlConnectionStringBuilder DataConnectionSettings(
        string? data, NpgsqlConnectionStringBuilder owner, NpgsqlConnectionStringBuilder verifier)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            throw new InvalidDataException("A dedicated data migration PostgreSQL connection secret is required.");
        }

        var settings = ParseSettings(data);
        if (string.Equals(settings.Username, owner.Username, StringComparison.Ordinal)
            || string.Equals(settings.Username, verifier.Username, StringComparison.Ordinal))
        {
            throw new InvalidDataException("The data migrator requires a separate PostgreSQL login.");
        }

        RequireSameTarget(owner, settings);
        return settings;
    }

    /// <summary>Checks the connected principals and their live PostgreSQL target.</summary>
    internal static async Task ValidateSessions(
        NpgsqlConnection owner, NpgsqlConnection verifier, NpgsqlConnection? data, CancellationToken ct)
    {
        const string invalidPrincipals = "Migration requires separate scoped logins on the same PostgreSQL primary.";
        if (owner.UserName is not { } ownerLogin || verifier.UserName is not { } verifierLogin)
        {
            throw new InvalidDataException(invalidPrincipals);
        }

        var dataLogin = data?.UserName;
        var ownerTarget = await ReadTarget(owner, [], ct);
        var verifierTarget = await ReadTarget(verifier, dataLogin is null ? [ownerLogin] : [ownerLogin, dataLogin], ct);
        var dataTarget = data is null ? ownerTarget : await ReadTarget(data, [ownerLogin, verifierLogin], ct);
        if (ownerTarget is null || !string.Equals(ownerTarget, verifierTarget, StringComparison.Ordinal)
            || !string.Equals(ownerTarget, dataTarget, StringComparison.Ordinal))
        {
            throw new InvalidDataException(invalidPrincipals);
        }
    }

    private static async Task<string?> ReadTarget(NpgsqlConnection connection, string[] prohibitedRoles, CancellationToken ct)
    {
        const string query = """
            SELECT pg_catalog.concat(current_database(), '/', pg_catalog.inet_server_addr(), '/', pg_catalog.inet_server_port())
            FROM pg_catalog.pg_roles
            WHERE rolname = current_user AND current_user = session_user AND current_user = $1
              AND NOT (rolsuper OR rolcreatedb OR rolcreaterole OR rolreplication OR rolbypassrls)
              AND NOT pg_catalog.pg_is_in_recovery()
              AND NOT EXISTS (
                  SELECT 1 FROM pg_catalog.unnest($2) AS prohibited(role)
                  WHERE pg_catalog.pg_has_role(prohibited.role, 'MEMBER')
              );
            """;
        await using var command = new NpgsqlCommand(query, connection);
        Bind(command, connection.UserName!, prohibitedRoles);
        return (string?)await command.ExecuteScalarAsync(ct);
    }

    /// <summary>Acquires a database-scoped lock for the context through the end of the owner session.</summary>
    internal static async Task LockContext(NpgsqlConnection owner, string context, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("SELECT pg_catalog.pg_try_advisory_lock($1);", owner);
        Bind(command, LockKey("context", context));
        if (await command.ExecuteScalarAsync(ct) is not true)
        {
            throw new InvalidDataException("The migration context is already locked.");
        }
    }

    /// <summary>Fences a data transaction against a successor owner after a lost owner session.</summary>
    internal static async Task LockBatch(NpgsqlConnection connection, NpgsqlTransaction transaction, string context, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("SELECT pg_catalog.pg_try_advisory_xact_lock($1);", connection, transaction);
        Bind(command, LockKey("batch", context));
        if (await command.ExecuteScalarAsync(ct) is not true)
        {
            throw new InvalidDataException("The migration context is already locked by a data batch.");
        }
    }

    /// <summary>Installs session-local invoker functions that keep reviewed scripts inside PostgreSQL transactions.</summary>
    internal static async Task InstallScriptFunctions(NpgsqlConnection owner, NpgsqlConnection verifier, CancellationToken ct)
    {
        await using var expansion = new NpgsqlCommand(ExpansionFunction, owner);
        await expansion.ExecuteNonQueryAsync(ct);
        await using var verification = new NpgsqlCommand(VerificationFunction, verifier);
        await verification.ExecuteNonQueryAsync(ct);
    }

    /// <summary>Executes expansion SQL in the supplied transaction.</summary>
    internal static async Task Expand(NpgsqlConnection owner, NpgsqlTransaction transaction, MigrationStep step, string sql, CancellationToken ct)
    {
        await SetTimeouts(owner, transaction, step, ct);
        await using var command = new NpgsqlCommand("SELECT pg_temp.execute_governed_migration($1);", owner, transaction);
        Bind(command, sql);
        command.CommandTimeout = step.TimeoutSeconds;
        await command.ExecuteNonQueryAsync(ct);
    }

    /// <summary>Executes a gate in a read-only transaction and accepts exactly one true PostgreSQL Boolean.</summary>
    internal static async Task<bool> Verify(NpgsqlConnection verifier, MigrationStep step, string sql, CancellationToken ct)
    {
        await using var transaction = await verifier.BeginTransactionAsync(ct);
        await using var readOnly = new NpgsqlCommand("SET TRANSACTION READ ONLY;", verifier, transaction);
        await readOnly.ExecuteNonQueryAsync(ct);
        await SetTimeouts(verifier, transaction, step, ct);

        await using var command = new NpgsqlCommand("""
            SELECT count(*) = 1 AND pg_catalog.bool_and(result) IS TRUE
            FROM pg_temp.verify_governed_migration($1) AS result;
            """, verifier, transaction);
        Bind(command, sql);
        command.CommandTimeout = step.TimeoutSeconds;
        var passed = await command.ExecuteScalarAsync(ct) is true;
        await transaction.CommitAsync(ct);
        return passed;
    }

    /// <summary>Binds positional values to a command with a fixed SQL template.</summary>
    internal static void Bind(NpgsqlCommand command, params object[] values)
    {
        foreach (var value in values)
        {
            command.Parameters.AddWithValue(value);
        }
    }

    private static NpgsqlConnectionStringBuilder ParseSettings(string? connection)
    {
        if (string.IsNullOrWhiteSpace(connection))
        {
            throw new InvalidDataException("Both migration PostgreSQL connection secrets are required.");
        }

        var supplied = new DbConnectionStringBuilder { ConnectionString = connection };
        // Npgsql 11 removed this option. Governed migrations use dedicated sessions.
        if (supplied.Remove("Multiplexing"))
        {
            connection = supplied.ConnectionString;
        }

        var settings = new NpgsqlConnectionStringBuilder(connection)
        {
            Pooling = false,
            Enlist = false,
            IncludeErrorDetail = false,
            LogParameters = false,
            Timeout = 15,
            CommandTimeout = 30,
            CancellationTimeout = 2000
        };
        if (string.IsNullOrWhiteSpace(settings.Host) || settings.Host.Contains(',', StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(settings.Database) || string.IsNullOrWhiteSpace(settings.Username))
        {
            throw new InvalidDataException("Migration PostgreSQL secrets require one explicit host, database and login.");
        }

        return settings;
    }

    /// <summary>Applies the reviewed server statement and lock budgets to this transaction.</summary>
    internal static async Task SetTimeouts(
        NpgsqlConnection connection, NpgsqlTransaction transaction, MigrationStep step, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("""
            SELECT pg_catalog.set_config('statement_timeout', $1, true),
                   pg_catalog.set_config('lock_timeout', $2, true);
            """, connection, transaction);
        Bind(command,
            step.TimeoutSeconds.ToString(CultureInfo.InvariantCulture) + "s",
            step.LockTimeoutSeconds.ToString(CultureInfo.InvariantCulture) + "s");
        await command.ExecuteNonQueryAsync(ct);
    }

    private static void RequireSameTarget(NpgsqlConnectionStringBuilder owner, NpgsqlConnectionStringBuilder other)
    {
        if (!string.Equals(owner.Host, other.Host, StringComparison.OrdinalIgnoreCase)
            || owner.Port != other.Port
            || !string.Equals(owner.Database, other.Database, StringComparison.Ordinal))
        {
            throw new InvalidDataException("Migration logins must connect to the same explicit PostgreSQL target.");
        }
    }

    private static long LockKey(string purpose, string context)
    {
        // Keep the existing context key stable while domain-separating the transaction fence.
        var label = purpose == "context" ? $"viajantes:governed-migration:{context}" : $"viajantes:governed-migration:batch:{context}";
        return BinaryPrimitives.ReadInt64BigEndian(SHA256.HashData(Encoding.UTF8.GetBytes(label)));
    }
}
