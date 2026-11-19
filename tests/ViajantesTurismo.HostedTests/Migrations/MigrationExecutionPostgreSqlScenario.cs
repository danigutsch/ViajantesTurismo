using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
        await ExecuteControl($"""
            CREATE ROLE {Owner} LOGIN PASSWORD '{password}';
            CREATE ROLE {Verifier} LOGIN PASSWORD '{password}';
            CREATE SCHEMA governed_migration AUTHORIZATION {Owner};
            CREATE SCHEMA migration_fixture AUTHORIZATION {Owner};
            GRANT USAGE ON SCHEMA migration_fixture TO {Verifier};
            ALTER DEFAULT PRIVILEGES FOR ROLE {Owner} IN SCHEMA migration_fixture GRANT SELECT ON TABLES TO {Verifier};
            CREATE TABLE migration_fixture.gate (ready boolean NOT NULL);
            INSERT INTO migration_fixture.gate VALUES (false);
            ALTER TABLE migration_fixture.gate OWNER TO {Owner};
            GRANT SELECT ON migration_fixture.gate TO {Verifier};
            """, ct);
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
    public async Task ReleaseRetry(CancellationToken ct) => await ExecuteControl("UPDATE migration_fixture.gate SET ready = true;", ct);

    /// <summary>Gives the verifier a role membership that the executor must refuse.</summary>
    public async Task GrantOwnerMembershipToVerifier(CancellationToken ct) =>
        await ExecuteControl($"GRANT {Owner} TO {Verifier};", ct);

    /// <summary>Reads a test assertion from PostgreSQL as the fixture administrator.</summary>
    [SuppressMessage("Security", "CA2100", Justification = "Only constant fixture assertions call this method.")]
    public async Task<T> Read<T>(string sql, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand(sql, connection);
        return await command.ExecuteScalarAsync(ct) is T result ? result : throw new InvalidOperationException("Unexpected test query result.");
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
        await ExecuteControl($"SELECT pg_catalog.pg_terminate_backend({pid.ToString(CultureInfo.InvariantCulture)});", ct);
    }

    /// <summary>Waits until the executor is blocked at the gate.</summary>
    public async Task<int> AwaitBlockedExecutor(CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(50));
        do
        {
            var pid = await Read<int>("""
                SELECT COALESCE(max(pid), 0) FROM pg_catalog.pg_locks
                WHERE locktype = 'advisory' AND NOT granted AND classid = 0 AND objid = 813043
                  AND database = (SELECT oid FROM pg_catalog.pg_database WHERE datname = current_database());
                """, timeout.Token);
            if (pid != 0)
            {
                return pid;
            }
        } while (await timer.WaitForNextTickAsync(timeout.Token));

        throw new InvalidOperationException("The migration did not wait at its test lock.");
    }

    /// <inheritdoc />
    [SuppressMessage("Security", "CA2100", Justification = "Role identifiers contain only a fixed prefix and generated hexadecimal digits.")]
    public async ValueTask DisposeAsync()
    {
        var control = new NpgsqlConnectionStringBuilder(database.ConnectionString) { Database = "postgres", Pooling = false };
        try
        {
            await database.DisposeAsync();
            await using var connection = new NpgsqlConnection(control.ConnectionString);
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var drop = new NpgsqlCommand($"DROP ROLE IF EXISTS {Verifier}; DROP ROLE IF EXISTS {Owner};", connection);
            await drop.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
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

    [SuppressMessage("Security", "CA2100", Justification = "SQL uses only constants, generated hexadecimal values, and a numeric PostgreSQL process ID.")]
    private async Task ExecuteControl(string sql, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static string Digest(string content) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
}
