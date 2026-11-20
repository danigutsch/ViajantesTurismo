using System.Buffers.Binary;
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

        if (!string.Equals(ownerSettings.Host, verifierSettings.Host, StringComparison.OrdinalIgnoreCase)
            || ownerSettings.Port != verifierSettings.Port
            || !string.Equals(ownerSettings.Database, verifierSettings.Database, StringComparison.Ordinal))
        {
            throw new InvalidDataException("Migration owner and verifier must connect to the same explicit PostgreSQL target.");
        }

        return (ownerSettings, verifierSettings);
    }

    /// <summary>Checks the connected principals and their live PostgreSQL target.</summary>
    internal static async Task ValidateSessions(NpgsqlConnection owner, NpgsqlConnection verifier, CancellationToken ct)
    {
        const string invalidPrincipals = "Migration requires separate scoped logins on the same PostgreSQL primary.";
        if (owner.UserName is not { } ownerLogin || verifier.UserName is not { } verifierLogin)
        {
            throw new InvalidDataException(invalidPrincipals);
        }

        const string query = """
            SELECT pg_catalog.concat(current_database(), '/', pg_catalog.inet_server_addr(), '/', pg_catalog.inet_server_port())
            FROM pg_catalog.pg_roles
            WHERE rolname = current_user AND current_user = session_user AND current_user = $1
              AND NOT (rolsuper OR rolcreatedb OR rolcreaterole OR rolreplication OR rolbypassrls)
              AND NOT pg_catalog.pg_is_in_recovery()
              AND (current_user = $2 OR NOT pg_catalog.pg_has_role($2, 'MEMBER'));
            """;
        await using var ownerCheck = new NpgsqlCommand(query, owner);
        Bind(ownerCheck, ownerLogin, ownerLogin);
        await using var verifierCheck = new NpgsqlCommand(query, verifier);
        Bind(verifierCheck, verifierLogin, ownerLogin);
        var ownerTarget = await ownerCheck.ExecuteScalarAsync(ct);
        var verifierTarget = await verifierCheck.ExecuteScalarAsync(ct);
        if (ownerTarget is not string ownerIdentity || verifierTarget is not string verifierIdentity
            || !string.Equals(ownerIdentity, verifierIdentity, StringComparison.Ordinal))
        {
            throw new InvalidDataException(invalidPrincipals);
        }
    }

    /// <summary>Acquires a database-scoped lock for the context through the end of the owner session.</summary>
    internal static async Task LockContext(NpgsqlConnection owner, string context, CancellationToken ct)
    {
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes("viajantes:governed-migration:" + context));
        var key = BinaryPrimitives.ReadInt64BigEndian(digest);
        await using var command = new NpgsqlCommand("SELECT pg_catalog.pg_try_advisory_lock($1);", owner);
        Bind(command, key);
        if (await command.ExecuteScalarAsync(ct) is not true)
        {
            throw new InvalidDataException("The migration context is already locked.");
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

    /// <summary>Binds the positional values for a command with a fixed SQL template.</summary>
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

        var settings = new NpgsqlConnectionStringBuilder(connection)
        {
            Pooling = false,
            Multiplexing = false,
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

    private static async Task SetTimeouts(
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
}
