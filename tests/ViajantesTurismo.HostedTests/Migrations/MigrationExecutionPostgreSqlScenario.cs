using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Npgsql;
using PandaBear.Npgsql.Testing;

namespace ViajantesTurismo.HostedTests.Migrations;

/// <summary>Owns a PostgreSQL database, two limited principals, and synthetic migration files.</summary>
internal sealed class MigrationExecutionPostgreSqlScenario(PostgreSqlTestDatabaseLease database) : IAsyncDisposable
{
    private const int GateKey = 813043;
    private readonly DirectoryInfo directory = Directory.CreateTempSubdirectory("vt-governed-migration-");
    private readonly string suffix = Guid.NewGuid().ToString("N");
    private readonly string password = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));

    private string Owner => "mig_owner_" + suffix;
    private string Verifier => "mig_check_" + suffix;

    /// <summary>Provisions separate principals and the schemas used by the test.</summary>
    public async Task Provision(CancellationToken ct)
    {
        var define = new NpgsqlBatchCommand("""
            CREATE OR REPLACE FUNCTION pg_temp.provision_migration_fixture(owner_role text, verifier_role text, role_password text)
            RETURNS void LANGUAGE plpgsql SET search_path = pg_catalog, pg_temp AS $fixture$
            BEGIN
                EXECUTE pg_catalog.format('CREATE ROLE %I LOGIN PASSWORD %L', owner_role, role_password);
                EXECUTE pg_catalog.format('CREATE ROLE %I LOGIN PASSWORD %L', verifier_role, role_password);
                EXECUTE pg_catalog.format('CREATE SCHEMA governed_migration AUTHORIZATION %I', owner_role);
                EXECUTE pg_catalog.format('CREATE SCHEMA migration_fixture AUTHORIZATION %I', owner_role);
                EXECUTE pg_catalog.format('GRANT USAGE ON SCHEMA migration_fixture TO %I', verifier_role);
                EXECUTE pg_catalog.format(
                    'ALTER DEFAULT PRIVILEGES FOR ROLE %I IN SCHEMA migration_fixture GRANT SELECT ON TABLES TO %I',
                    owner_role, verifier_role);
                CREATE TABLE migration_fixture.gate (ready boolean NOT NULL);
                INSERT INTO migration_fixture.gate VALUES (false);
                EXECUTE pg_catalog.format('ALTER TABLE migration_fixture.gate OWNER TO %I', owner_role);
                EXECUTE pg_catalog.format('GRANT SELECT ON migration_fixture.gate TO %I', verifier_role);
            END;
            $fixture$;
            """);
        var provision = new NpgsqlBatchCommand("SELECT pg_temp.provision_migration_fixture($1, $2, $3);");
        provision.Parameters.Add(new NpgsqlParameter<string> { TypedValue = Owner });
        provision.Parameters.Add(new NpgsqlParameter<string> { TypedValue = Verifier });
        provision.Parameters.Add(new NpgsqlParameter<string> { TypedValue = password });
        await ExecuteRoleStatement(database.ConnectionString, define, provision, ct);
    }

    /// <summary>Writes a manifest and the exact SQL bytes for the supplied steps.</summary>
    public async Task<string> WriteManifest(
        string migrationId, IReadOnlyList<MigrationExecutionTestStep> steps, CancellationToken ct, string recoveryKind = "forwardOnly")
    {
        var folder = directory.CreateSubdirectory(migrationId).FullName;
        var phaseNames = new[] { "expand", "data", "verify", "contract" };
        var artifacts = new List<object>();
        for (var index = 0; index < steps.Count; index++)
        {
            var step = steps[index];
            var order = index + 1;
            var name = $"step-{order.ToString(CultureInfo.InvariantCulture)}.sql";
            await File.WriteAllTextAsync(Path.Combine(folder, name), step.Sql, ct);
            artifacts.Add(new
            {
                phase = step.Phase,
                order,
                script = name,
                sha256 = Digest(step.Sql),
                transaction = step.Transaction,
                timeoutSeconds = step.TimeoutSeconds,
                lockTimeoutSeconds = step.LockTimeoutSeconds,
                destructive = step.Phase == "contract",
                checkpoint = step.Transaction == "batched" ? "test-checkpoint" : null,
                verify = step.Transaction == "atomic" ? null : new { path = name, sha256 = Digest(step.Sql) }
            });
        }

        const string evidence = "Test-owned forward recovery rehearsal.";
        await File.WriteAllTextAsync(Path.Combine(folder, "recovery.md"), evidence, ct);
        const string recoverySql = "SELECT true;";
        var recoveryScript = default(object);
        if (recoveryKind == "reversible")
        {
            await File.WriteAllTextAsync(Path.Combine(folder, "recovery.sql"), recoverySql, ct);
            recoveryScript = new { path = "recovery.sql", sha256 = Digest(recoverySql) };
        }

        var manifest = JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            migrationId,
            context = "catalog",
            sourceReleases = new[] { "1.0" },
            targetRelease = "1.1",
            steps = artifacts,
            omittedPhases = phaseNames.Where(phase => steps.All(step => step.Phase != phase))
                .Select(phase => new { phase, reason = "No work in this phase." }).ToArray(),
            recovery = new { kind = recoveryKind, evidence = new { path = "recovery.md", sha256 = Digest(evidence) }, script = recoveryScript }
        });
        var path = Path.Combine(folder, "manifest.json");
        await File.WriteAllTextAsync(path, manifest, ct);
        return path;
    }

    /// <summary>Runs the real migration service command with the fixture's credentials.</summary>
    public async Task<(int ExitCode, string Output, string Error)> Run(string manifest, CancellationToken ct, bool sameRole = false)
    {
        var settings = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = directory.FullName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        settings.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "ViajantesTurismo.MigrationService.dll"));
        settings.ArgumentList.Add("execute");
        settings.ArgumentList.Add(manifest);
        settings.ArgumentList.Add("--from");
        settings.ArgumentList.Add("1.0");
        settings.Environment["VT_MIGRATION_OWNER_CONNECTION"] = ConnectionString(Owner);
        settings.Environment["VT_MIGRATION_VERIFIER_CONNECTION"] = ConnectionString(sameRole ? Owner : Verifier);

        using var process = Process.Start(settings) ?? throw new InvalidOperationException("Migration process did not start.");
        var output = process.StandardOutput.ReadToEndAsync(ct);
        var error = process.StandardError.ReadToEndAsync(ct);
        try
        {
            await process.WaitForExitAsync(ct);
            return (process.ExitCode, await output, await error);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(CancellationToken.None);
            }
        }
    }

    /// <summary>Releases a test-owned precondition used to prove atomic retry.</summary>
    public async Task ReleaseRetry(CancellationToken ct) =>
        await ExecuteControl(new NpgsqlCommand("UPDATE migration_fixture.gate SET ready = true;"), ct);

    /// <summary>Gives the verifier a role membership that the executor must refuse.</summary>
    public async Task GrantOwnerMembershipToVerifier(CancellationToken ct)
    {
        var define = new NpgsqlBatchCommand("""
            CREATE OR REPLACE FUNCTION pg_temp.grant_migration_fixture_role(owner_role text, verifier_role text)
            RETURNS void LANGUAGE plpgsql SET search_path = pg_catalog, pg_temp AS $fixture$
            BEGIN
                EXECUTE pg_catalog.format('GRANT %I TO %I', owner_role, verifier_role);
            END;
            $fixture$;
            """);
        var grant = new NpgsqlBatchCommand("SELECT pg_temp.grant_migration_fixture_role($1, $2);");
        grant.Parameters.Add(new NpgsqlParameter<string> { TypedValue = Owner });
        grant.Parameters.Add(new NpgsqlParameter<string> { TypedValue = Verifier });
        await ExecuteRoleStatement(database.ConnectionString, define, grant, ct);
    }

    /// <summary>Executes and disposes a PostgreSQL test assertion as the fixture administrator.</summary>
    public async Task<T> Read<T>(NpgsqlCommand command, CancellationToken ct)
    {
        await using var assertion = command;
        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync(ct);
        assertion.Connection = connection;
        return await assertion.ExecuteScalarAsync(ct) is T result ? result : throw new InvalidOperationException("Unexpected test query result.");
    }

    /// <summary>Returns a session that blocks the test migration at a known PostgreSQL advisory lock.</summary>
    public async Task<NpgsqlConnection> HoldGate(CancellationToken ct)
    {
        var settings = new NpgsqlConnectionStringBuilder(database.ConnectionString) { Pooling = false };
        var connection = new NpgsqlConnection(settings.ConnectionString);
        try
        {
            await connection.OpenAsync(ct);
            await using var command = new NpgsqlCommand("SELECT pg_catalog.pg_advisory_lock($1)", connection);
            command.Parameters.AddWithValue((long)GateKey);
            await command.ExecuteNonQueryAsync(ct);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    /// <summary>Waits for an observed server lock request and terminates only that test-owned backend.</summary>
    public async Task InterruptBlockedExecutor(CancellationToken ct)
    {
        var pid = await AwaitBlockedExecutor(ct);
        var terminate = new NpgsqlCommand("SELECT pg_catalog.pg_terminate_backend($1);");
        terminate.Parameters.Add(new NpgsqlParameter<int> { TypedValue = pid });
        await ExecuteControl(terminate, ct);
    }

    /// <summary>Waits until the executor is blocked at the gate.</summary>
    public async Task<int> AwaitBlockedExecutor(CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(50));
        do
        {
            var pid = await Read<int>(new NpgsqlCommand("""
                SELECT COALESCE(max(pid), 0) FROM pg_catalog.pg_locks
                WHERE locktype = 'advisory' AND NOT granted AND classid = 0 AND objid = 813043
                  AND database = (SELECT oid FROM pg_catalog.pg_database WHERE datname = current_database());
                """), timeout.Token);
            if (pid != 0)
            {
                return pid;
            }
        } while (await timer.WaitForNextTickAsync(timeout.Token));

        throw new InvalidOperationException("The migration did not wait at its test lock.");
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        var control = new NpgsqlConnectionStringBuilder(database.ConnectionString) { Database = "postgres", Pooling = false };
        try
        {
            await database.DisposeAsync();
            var define = new NpgsqlBatchCommand("""
                CREATE FUNCTION pg_temp.drop_migration_fixture_roles(owner_role text, verifier_role text)
                RETURNS void LANGUAGE plpgsql SET search_path = pg_catalog, pg_temp AS $fixture$
                BEGIN
                    EXECUTE pg_catalog.format('DROP ROLE IF EXISTS %I', verifier_role);
                    EXECUTE pg_catalog.format('DROP ROLE IF EXISTS %I', owner_role);
                END;
                $fixture$;
                """);
            var drop = new NpgsqlBatchCommand("SELECT pg_temp.drop_migration_fixture_roles($1, $2);");
            drop.Parameters.Add(new NpgsqlParameter<string> { TypedValue = Owner });
            drop.Parameters.Add(new NpgsqlParameter<string> { TypedValue = Verifier });
            await ExecuteRoleStatement(control.ConnectionString, define, drop, TestContext.Current.CancellationToken);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private string ConnectionString(string role) => new NpgsqlConnectionStringBuilder(database.ConnectionString)
    {
        Username = role,
        Password = password,
        Pooling = false
    }.ConnectionString;

    private async Task ExecuteControl(NpgsqlCommand command, CancellationToken ct)
    {
        await using var control = command;
        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync(ct);
        control.Connection = connection;
        await control.ExecuteNonQueryAsync(ct);
    }

    private static async Task ExecuteRoleStatement(
        string connectionString, NpgsqlBatchCommand definition, NpgsqlBatchCommand invocation, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var batch = new NpgsqlBatch(connection) { BatchCommands = { definition, invocation } };
        await batch.ExecuteNonQueryAsync(ct);
    }

    private static string Digest(string content) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
}
