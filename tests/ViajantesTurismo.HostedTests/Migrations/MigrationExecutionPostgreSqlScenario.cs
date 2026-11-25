using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Npgsql;
using SharedKernel.Npgsql.Testing;

namespace ViajantesTurismo.HostedTests.Migrations;

/// <summary>Owns a PostgreSQL database, scoped principals, and synthetic migration files.</summary>
internal sealed class MigrationExecutionPostgreSqlScenario(PostgreSqlTestDatabaseLease database) : IAsyncDisposable
{
    private const int GateKey = 813043;
    private readonly DirectoryInfo directory = Directory.CreateTempSubdirectory("vt-governed-migration-");
    private readonly string suffix = Guid.NewGuid().ToString("N");
    private readonly string password = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));

    private string Owner => "mig_owner_" + suffix;
    private string Verifier => "mig_check_" + suffix;
    private string DataMigrator => "mig_data_" + suffix;
    private string Runtime => "mig_runtime_" + suffix;
    private string BrandingOwner => "mig_branding_owner_" + suffix;
    private string BrandingVerifier => "mig_branding_check_" + suffix;
    private string BrandingDataMigrator => "mig_branding_data_" + suffix;

    /// <summary>Provisions separate principals and the schemas used by the test.</summary>
    public async Task Provision(CancellationToken ct)
    {
        var define = new NpgsqlBatchCommand("""
            CREATE OR REPLACE FUNCTION pg_temp.provision_migration_fixture(
                owner_role text, verifier_role text, data_role text, role_password text)
            RETURNS void LANGUAGE plpgsql SET search_path = pg_catalog, pg_temp AS $fixture$
            BEGIN
                EXECUTE pg_catalog.format('CREATE ROLE %I LOGIN PASSWORD %L', owner_role, role_password);
                EXECUTE pg_catalog.format('CREATE ROLE %I LOGIN PASSWORD %L', verifier_role, role_password);
                EXECUTE pg_catalog.format('CREATE ROLE %I LOGIN PASSWORD %L', data_role, role_password);
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
        var provision = new NpgsqlBatchCommand("SELECT pg_temp.provision_migration_fixture($1, $2, $3, $4);");
        provision.Parameters.Add(new NpgsqlParameter<string> { TypedValue = Owner });
        provision.Parameters.Add(new NpgsqlParameter<string> { TypedValue = Verifier });
        provision.Parameters.Add(new NpgsqlParameter<string> { TypedValue = DataMigrator });
        provision.Parameters.Add(new NpgsqlParameter<string> { TypedValue = password });
        await ExecuteRoleStatement(database.ConnectionString, define, provision, ct);
    }

    /// <summary>Creates a second context and separate principals inside the same leased database.</summary>
    public async Task ProvisionSharedBranding(CancellationToken ct)
    {
        var define = new NpgsqlBatchCommand("""
            CREATE FUNCTION pg_temp.provision_shared_branding(catalog_owner text, branding_owner text, branding_verifier text, branding_data text, role_password text)
            RETURNS void LANGUAGE plpgsql SET search_path = pg_catalog, pg_temp AS $fixture$
            BEGIN
                EXECUTE pg_catalog.format('CREATE ROLE %I LOGIN PASSWORD %L', branding_owner, role_password);
                EXECUTE pg_catalog.format('CREATE ROLE %I LOGIN PASSWORD %L', branding_verifier, role_password);
                EXECUTE pg_catalog.format('CREATE ROLE %I LOGIN PASSWORD %L', branding_data, role_password);
                EXECUTE pg_catalog.format('CREATE SCHEMA branding_fixture AUTHORIZATION %I', branding_owner);
                EXECUTE pg_catalog.format('CREATE SCHEMA governed_migration_catalog AUTHORIZATION %I', catalog_owner);
                EXECUTE pg_catalog.format('CREATE SCHEMA governed_migration_branding AUTHORIZATION %I', branding_owner);
                CREATE TABLE branding_fixture.backfill (id bigint PRIMARY KEY, updates integer NOT NULL DEFAULT 0);
                INSERT INTO branding_fixture.backfill (id) VALUES (1), (2), (3);
                EXECUTE pg_catalog.format('ALTER TABLE branding_fixture.backfill OWNER TO %I', branding_owner);
                EXECUTE pg_catalog.format('GRANT USAGE ON SCHEMA branding_fixture TO %I, %I', branding_verifier, branding_data);
                EXECUTE pg_catalog.format('GRANT SELECT ON branding_fixture.backfill TO %I', branding_verifier);
                EXECUTE pg_catalog.format('GRANT SELECT, UPDATE (updates) ON branding_fixture.backfill TO %I', branding_data);
                EXECUTE pg_catalog.format(
                    'ALTER DEFAULT PRIVILEGES FOR ROLE %I IN SCHEMA branding_fixture GRANT SELECT ON TABLES TO %I',
                    branding_owner, branding_verifier);
            END;
            $fixture$;
            """);
        var provision = new NpgsqlBatchCommand("SELECT pg_temp.provision_shared_branding($1, $2, $3, $4, $5);");
        provision.Parameters.Add(new NpgsqlParameter<string> { TypedValue = Owner });
        provision.Parameters.Add(new NpgsqlParameter<string> { TypedValue = BrandingOwner });
        provision.Parameters.Add(new NpgsqlParameter<string> { TypedValue = BrandingVerifier });
        provision.Parameters.Add(new NpgsqlParameter<string> { TypedValue = BrandingDataMigrator });
        provision.Parameters.Add(new NpgsqlParameter<string> { TypedValue = password });
        await ExecuteRoleStatement(database.ConnectionString, define, provision, ct);
    }

    /// <summary>Opens a separately scoped schema owner session for the shared database proof.</summary>
    public async Task<NpgsqlConnection> OpenMigrationOwner(bool asBranding, CancellationToken ct) =>
        await Open(ConnectionString(asBranding ? BrandingOwner : Owner), ct);

    /// <summary>Opens either context's independently granted data login.</summary>
    public async Task<NpgsqlConnection> OpenMigrationData(bool asBranding, CancellationToken ct) =>
        await Open(ConnectionString(asBranding ? BrandingDataMigrator : DataMigrator), ct);

    /// <summary>Grants the selected fixture's migration owner the privileges needed to initialize a fresh database.</summary>
    public async Task GrantBaselineCreation(bool asBranding, CancellationToken ct)
    {
        var define = new NpgsqlBatchCommand("""
            CREATE FUNCTION pg_temp.grant_baseline_creation(owner_role text)
            RETURNS void LANGUAGE plpgsql SET search_path = pg_catalog, pg_temp AS $fixture$
            BEGIN
                EXECUTE pg_catalog.format('GRANT CREATE ON DATABASE %I TO %I', current_database(), owner_role);
                EXECUTE pg_catalog.format('GRANT CREATE ON SCHEMA public TO %I', owner_role);
            END;
            $fixture$;
            """);
        var provision = new NpgsqlBatchCommand("SELECT pg_temp.grant_baseline_creation($1);");
        provision.Parameters.Add(new NpgsqlParameter<string> { TypedValue = asBranding ? BrandingOwner : Owner });
        await ExecuteRoleStatement(database.ConnectionString, define, provision, ct);
    }

    /// <summary>Allows the governed role to append the Admin expansion's existing migration history.</summary>
    public async Task GrantAdminHistoryAppend(CancellationToken ct)
    {
        var define = new NpgsqlBatchCommand("""
            CREATE FUNCTION pg_temp.grant_admin_history_append(owner_role text)
            RETURNS void LANGUAGE plpgsql SET search_path = pg_catalog, pg_temp AS $fixture$
            BEGIN
                EXECUTE pg_catalog.format('GRANT INSERT ON public."__EFMigrationsHistory" TO %I', owner_role);
            END;
            $fixture$;
            """);
        var grant = new NpgsqlBatchCommand("SELECT pg_temp.grant_admin_history_append($1);");
        grant.Parameters.Add(new NpgsqlParameter<string> { TypedValue = Owner });
        await ExecuteRoleStatement(database.ConnectionString, define, grant, ct);
    }

    /// <summary>Provisions a disposable scheduler login that can append audit but cannot edit or delete it.</summary>
    public async Task ProvisionAdminSchedulerRuntime(CancellationToken ct)
    {
        var define = new NpgsqlBatchCommand("""
            CREATE FUNCTION pg_temp.provision_admin_scheduler_runtime(runtime_role text, role_password text)
            RETURNS void LANGUAGE plpgsql SET search_path = pg_catalog, pg_temp AS $fixture$
            BEGIN
                EXECUTE pg_catalog.format('CREATE ROLE %I LOGIN NOINHERIT PASSWORD %L', runtime_role, role_password);
                EXECUTE pg_catalog.format('GRANT USAGE ON SCHEMA document_maintenance TO %I', runtime_role);
                EXECUTE pg_catalog.format(
                    'GRANT SELECT, INSERT, UPDATE ON document_maintenance.scheduler_jobs, document_maintenance.scheduler_executions TO %I',
                    runtime_role);
                EXECUTE pg_catalog.format('GRANT SELECT, INSERT ON document_maintenance.control_audit TO %I', runtime_role);
            END;
            $fixture$;
            """);
        var provision = new NpgsqlBatchCommand("SELECT pg_temp.provision_admin_scheduler_runtime($1, $2);");
        provision.Parameters.Add(new NpgsqlParameter<string> { TypedValue = Runtime });
        provision.Parameters.Add(new NpgsqlParameter<string> { TypedValue = password });
        await ExecuteRoleStatement(database.ConnectionString, define, provision, ct);
    }

    /// <summary>Pre-provisions Catalog's provider schema and grants the verifier metadata visibility.</summary>
    public async Task ProvisionCatalogEventStoreSchema(CancellationToken ct)
    {
        var define = new NpgsqlBatchCommand("""
            CREATE FUNCTION pg_temp.provision_catalog_event_store(owner_role text, verifier_role text)
            RETURNS void LANGUAGE plpgsql SET search_path = pg_catalog, pg_temp AS $fixture$
            BEGIN
                EXECUTE pg_catalog.format('CREATE SCHEMA event_sourcing AUTHORIZATION %I', owner_role);
                EXECUTE pg_catalog.format('GRANT USAGE ON SCHEMA event_sourcing TO %I', verifier_role);
            END;
            $fixture$;
            """);
        var provision = new NpgsqlBatchCommand("SELECT pg_temp.provision_catalog_event_store($1, $2);");
        provision.Parameters.Add(new NpgsqlParameter<string> { TypedValue = Owner });
        provision.Parameters.Add(new NpgsqlParameter<string> { TypedValue = Verifier });
        await ExecuteRoleStatement(database.ConnectionString, define, provision, ct);
    }

    /// <summary>Provisions a distinct, read-only fixture runtime for the Catalog events table.</summary>
    public async Task ProvisionCatalogEventRuntime(CancellationToken ct)
    {
        var define = new NpgsqlBatchCommand("""
            CREATE FUNCTION pg_temp.provision_catalog_event_runtime(runtime_role text, role_password text)
            RETURNS void LANGUAGE plpgsql SET search_path = pg_catalog, pg_temp AS $fixture$
            BEGIN
                EXECUTE pg_catalog.format('CREATE ROLE %I LOGIN NOINHERIT PASSWORD %L', runtime_role, role_password);
                EXECUTE pg_catalog.format('GRANT USAGE ON SCHEMA event_sourcing TO %I', runtime_role);
                EXECUTE pg_catalog.format('GRANT SELECT ON event_sourcing.events TO %I', runtime_role);
            END;
            $fixture$;
            """);
        var provision = new NpgsqlBatchCommand("SELECT pg_temp.provision_catalog_event_runtime($1, $2);");
        provision.Parameters.Add(new NpgsqlParameter<string> { TypedValue = Runtime });
        provision.Parameters.Add(new NpgsqlParameter<string> { TypedValue = password });
        await ExecuteRoleStatement(database.ConnectionString, define, provision, ct);
    }

    /// <summary>Provisions a synthetic backfill and grants the data principal scoped DML.</summary>
    public async Task ProvisionBatchData(CancellationToken ct)
    {
        var define = new NpgsqlBatchCommand("""
            CREATE OR REPLACE FUNCTION pg_temp.provision_migration_batch_data(owner_role text, verifier_role text, data_role text)
            RETURNS void LANGUAGE plpgsql SET search_path = pg_catalog, pg_temp AS $fixture$
            BEGIN
                CREATE TABLE migration_fixture.backfill (id bigint PRIMARY KEY, updates integer NOT NULL DEFAULT 0);
                INSERT INTO migration_fixture.backfill (id) VALUES (1), (2), (3);
                EXECUTE pg_catalog.format('ALTER TABLE migration_fixture.backfill OWNER TO %I', owner_role);
                EXECUTE pg_catalog.format('GRANT USAGE ON SCHEMA migration_fixture TO %I', data_role);
                EXECUTE pg_catalog.format('GRANT SELECT, UPDATE (updates) ON migration_fixture.backfill TO %I', data_role);
                EXECUTE pg_catalog.format('GRANT SELECT ON migration_fixture.backfill, migration_fixture.gate TO %I', verifier_role);
            END;
            $fixture$;
            """);
        var provision = new NpgsqlBatchCommand("SELECT pg_temp.provision_migration_batch_data($1, $2, $3);");
        provision.Parameters.Add(new NpgsqlParameter<string> { TypedValue = Owner });
        provision.Parameters.Add(new NpgsqlParameter<string> { TypedValue = Verifier });
        provision.Parameters.Add(new NpgsqlParameter<string> { TypedValue = DataMigrator });
        await ExecuteRoleStatement(database.ConnectionString, define, provision, ct);
    }

    /// <summary>Restores the Catalog rehearsal snapshot and assigns test-owned permissions.</summary>
    public async Task RestoreCatalogSnapshot(CancellationToken ct)
    {
        var restore = new NpgsqlBatchCommand("""
            CREATE FUNCTION pg_temp.restore_catalog_snapshot(snapshot text, owner_role text, verifier_role text,
                data_role text, runtime_role text, role_password text)
            RETURNS void LANGUAGE plpgsql SET search_path = pg_catalog, pg_temp AS $fixture$
            BEGIN
                EXECUTE snapshot;
                EXECUTE pg_catalog.format('CREATE ROLE %I LOGIN PASSWORD %L', runtime_role, role_password);
                REVOKE CREATE ON SCHEMA public FROM PUBLIC;
                EXECUTE pg_catalog.format('REVOKE TEMPORARY ON DATABASE %I FROM PUBLIC', current_database());
                EXECUTE pg_catalog.format('GRANT TEMPORARY ON DATABASE %I TO %I, %I, %I',
                    current_database(), owner_role, verifier_role, data_role);
                EXECUTE pg_catalog.format('ALTER TABLE public."PublicContent" OWNER TO %I', owner_role);
                EXECUTE pg_catalog.format('ALTER TABLE public."PublicContentVariants" OWNER TO %I', owner_role);
                EXECUTE pg_catalog.format('GRANT USAGE ON SCHEMA public TO %I, %I, %I, %I',
                    owner_role, verifier_role, data_role, runtime_role);
                EXECUTE pg_catalog.format('GRANT SELECT ON public."PublicContent", public."PublicContentVariants" TO %I, %I',
                    verifier_role, runtime_role);
                EXECUTE pg_catalog.format('GRANT SELECT, UPDATE ON public."PublicContent" TO %I', data_role);
            END;
            $fixture$;
            """);
        var provision = new NpgsqlBatchCommand("SELECT pg_temp.restore_catalog_snapshot($1, $2, $3, $4, $5, $6);");
        provision.Parameters.Add(new NpgsqlParameter<string> { TypedValue = CatalogMigrationRehearsalArtifacts.Snapshot });
        provision.Parameters.Add(new NpgsqlParameter<string> { TypedValue = Owner });
        provision.Parameters.Add(new NpgsqlParameter<string> { TypedValue = Verifier });
        provision.Parameters.Add(new NpgsqlParameter<string> { TypedValue = DataMigrator });
        provision.Parameters.Add(new NpgsqlParameter<string> { TypedValue = Runtime });
        provision.Parameters.Add(new NpgsqlParameter<string> { TypedValue = password });
        await ExecuteRoleStatement(database.ConnectionString, restore, provision, ct);
    }

    /// <summary>The connection used by the Catalog application reader during rehearsal.</summary>
    internal string RuntimeConnectionString => ConnectionString(Runtime);

    /// <summary>The digest of the synthetic snapshot bytes used in the Catalog rehearsal.</summary>
    internal static string CatalogSnapshotDigest => Digest(CatalogMigrationRehearsalArtifacts.Snapshot);

    /// <summary>Creates an ordinary table with a distinct application runtime principal.</summary>
    public async Task ProvisionOnlineData(CancellationToken ct)
    {
        var define = new NpgsqlBatchCommand("""
            CREATE FUNCTION pg_temp.provision_migration_online_data(owner_role text, runtime_role text, role_password text)
            RETURNS void LANGUAGE plpgsql SET search_path = pg_catalog, pg_temp AS $fixture$
            BEGIN
                EXECUTE pg_catalog.format('CREATE ROLE %I LOGIN PASSWORD %L', runtime_role, role_password);
                CREATE TABLE migration_fixture.online_entries (id integer PRIMARY KEY, category integer NOT NULL);
                INSERT INTO migration_fixture.online_entries VALUES (1, 10);
                EXECUTE pg_catalog.format('ALTER TABLE migration_fixture.online_entries OWNER TO %I', owner_role);
                EXECUTE pg_catalog.format('GRANT USAGE ON SCHEMA migration_fixture TO %I', runtime_role);
                EXECUTE pg_catalog.format('GRANT SELECT, INSERT ON migration_fixture.online_entries TO %I', runtime_role);
            END;
            $fixture$;
            """);
        var provision = new NpgsqlBatchCommand("SELECT pg_temp.provision_migration_online_data($1, $2, $3);");
        provision.Parameters.Add(new NpgsqlParameter<string> { TypedValue = Owner });
        provision.Parameters.Add(new NpgsqlParameter<string> { TypedValue = Runtime });
        provision.Parameters.Add(new NpgsqlParameter<string> { TypedValue = password });
        await ExecuteRoleStatement(database.ConnectionString, define, provision, ct);
    }

    /// <summary>Opens a dedicated fixture administrator session for transaction control.</summary>
    public async Task<NpgsqlConnection> OpenControl(CancellationToken ct)
    {
        var settings = new NpgsqlConnectionStringBuilder(database.ConnectionString) { Pooling = false };
        return await Open(settings.ConnectionString, ct);
    }

    /// <summary>Opens a database session with ordinary runtime grants.</summary>
    public async Task<NpgsqlConnection> OpenRuntime(CancellationToken ct) => await Open(ConnectionString(Runtime), ct);

    /// <summary>Waits for a concurrent index waiter owned by this migration fixture.</summary>
    public async Task AwaitOnlineWait(bool relationLock, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(50));
        do
        {
            var probe = new NpgsqlCommand("""
                SELECT EXISTS (
                    SELECT 1 FROM pg_catalog.pg_stat_activity AS activity
                    LEFT JOIN pg_catalog.pg_stat_progress_create_index AS progress ON progress.pid = activity.pid
                    WHERE activity.datname = current_database() AND activity.usename = $1
                      AND activity.query LIKE 'CREATE%INDEX CONCURRENTLY%'
                      AND (($2 AND activity.wait_event_type = 'Lock' AND activity.wait_event = 'relation')
                          OR (NOT $2 AND progress.phase = 'waiting for writers before build'))
                );
                """);
            probe.Parameters.Add(new NpgsqlParameter<string> { TypedValue = Owner });
            probe.Parameters.Add(new NpgsqlParameter<bool> { TypedValue = relationLock });
            if (await Read<bool>(probe, timeout.Token))
            {
                return;
            }
        } while (await timer.WaitForNextTickAsync(timeout.Token));

        throw new InvalidOperationException("The online migration did not wait at the expected lock boundary.");
    }

    /// <summary>Writes a manifest and the exact SQL bytes for the supplied steps.</summary>
    public async Task<string> WriteManifest(
        string migrationId, IReadOnlyList<MigrationExecutionTestStep> steps, CancellationToken ct, string recoveryKind = "forwardOnly",
        string recoverySql = "SELECT true;", string recoveryVerification = "", string extraSource = "", string context = "catalog", string journalSchema = "")
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
            var verifyName = $"verify-{order.ToString(CultureInfo.InvariantCulture)}.sql";
            if (step.Transaction != "atomic")
            {
                await File.WriteAllTextAsync(Path.Combine(folder, verifyName), step.VerifySql, ct);
            }

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
                verify = step.Transaction == "atomic" ? null : new { path = verifyName, sha256 = Digest(step.VerifySql) },
                onlineIndex = step.OnlineIndex
            });
        }

        const string evidence = "Test-owned forward recovery rehearsal.";
        await File.WriteAllTextAsync(Path.Combine(folder, "recovery.md"), evidence, ct);
        var recoveryScript = default(object);
        if (recoveryKind is "reversible" or "compensating")
        {
            await File.WriteAllTextAsync(Path.Combine(folder, "recovery.sql"), recoverySql, ct);
            recoveryScript = new { path = "recovery.sql", sha256 = Digest(recoverySql) };
        }

        var recoveryVerify = default(object);
        if (recoveryVerification.Length > 0)
        {
            await File.WriteAllTextAsync(Path.Combine(folder, "recovery-verify.sql"), recoveryVerification, ct);
            recoveryVerify = new { path = "recovery-verify.sql", sha256 = Digest(recoveryVerification) };
        }

        var manifest = JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            migrationId,
            context,
            sourceReleases = extraSource.Length == 0 ? new[] { "1.0" } : new[] { "1.0", extraSource },
            targetRelease = "1.1",
            steps = artifacts,
            omittedPhases = phaseNames.Where(phase => steps.All(step => step.Phase != phase))
                .Select(phase => new { phase, reason = "No work in this phase." }).ToArray(),
            recovery = new { kind = recoveryKind, evidence = new { path = "recovery.md", sha256 = Digest(evidence) }, script = recoveryScript, verify = recoveryVerify }
        });
        var path = Path.Combine(folder, "manifest.json");
        if (journalSchema.Length > 0)
        {
            var selectedManifest = JsonNode.Parse(manifest).ShouldNotBeNull().AsObject();
            selectedManifest["journalSchema"] = journalSchema;
            manifest = selectedManifest.ToJsonString();
        }

        await File.WriteAllTextAsync(path, manifest, ct);
        return path;
    }

    /// <summary>Writes the synthetic retirement receipt using this test's database identity.</summary>
    public async Task<(string Path, string Sha256)> WriteRetirement(
        string manifest, string migrationId, CancellationToken ct, string deploymentTarget = "synthetic-test", bool rehearsalPassed = true)
    {
        var content = await File.ReadAllTextAsync(manifest, ct);
        var evidence = await File.ReadAllTextAsync(Path.Combine(Path.GetDirectoryName(manifest).ShouldNotBeNull(), "recovery.md"), ct);
        var retirement = JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            context = "catalog",
            migrationId,
            manifestSha256 = Digest(content),
            targetRelease = "1.1",
            retiredSourceReleases = new[] { "1.0" },
            deploymentTarget,
            database = new NpgsqlConnectionStringBuilder(database.ConnectionString).Database,
            recoveryEvidenceSha256 = Digest(evidence),
            recoveryRehearsalPassed = rehearsalPassed
        });
        var file = Path.Combine(directory.FullName, "retirement.json");
        await File.WriteAllTextAsync(file, retirement, ct);
        return (file, Digest(retirement));
    }

    /// <summary>Runs the real migration service command with the fixture's credentials.</summary>
    public async Task<(int ExitCode, string Output, string Error)> Run(
        string manifest, CancellationToken ct, bool sameRole = false, bool dataUsesOwner = false,
        string action = "execute", (string Path, string Sha256)? retirement = null, bool asBranding = false)
    {
        var settings = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = directory.FullName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        settings.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "ViajantesTurismo.MigrationService.dll"));
        settings.ArgumentList.Add(action);
        settings.ArgumentList.Add(manifest);
        settings.ArgumentList.Add("--from");
        settings.ArgumentList.Add("1.0");
        if (retirement is { } proof)
        {
            settings.ArgumentList.Add("--retirement");
            settings.ArgumentList.Add(proof.Path);
            settings.ArgumentList.Add("--sha256");
            settings.ArgumentList.Add(proof.Sha256);
        }

        settings.Environment["VT_MIGRATION_DEPLOYMENT_TARGET"] = "synthetic-test";
        var owner = asBranding ? BrandingOwner : Owner;
        var verifier = asBranding ? BrandingVerifier : Verifier;
        settings.Environment["VT_MIGRATION_OWNER_CONNECTION"] = ConnectionString(owner);
        settings.Environment["VT_MIGRATION_VERIFIER_CONNECTION"] = ConnectionString(sameRole ? owner : verifier);
        var data = asBranding ? BrandingDataMigrator : DataMigrator;
        settings.Environment["VT_MIGRATION_DATA_CONNECTION"] = ConnectionString(dataUsesOwner ? owner : data);

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

    /// <summary>Executes and disposes a test assertion as the fixture administrator or data principal.</summary>
    public async Task<T> Read<T>(NpgsqlCommand command, CancellationToken ct, bool asDataMigrator = false)
    {
        await using var assertion = command;
        await using var connection = new NpgsqlConnection(asDataMigrator ? ConnectionString(DataMigrator) : database.ConnectionString);
        await connection.OpenAsync(ct);
        assertion.Connection = connection;
        return await assertion.ExecuteScalarAsync(ct) is T result ? result : throw new InvalidOperationException("Unexpected test query result.");
    }

    /// <summary>Returns a session that blocks the test migration at a known PostgreSQL advisory lock.</summary>
    public async Task<NpgsqlConnection> HoldGate(CancellationToken ct)
    {
        var connection = await OpenControl(ct);
        try
        {
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

    /// <summary>Ends the owner session while leaving the isolated data batch connection running.</summary>
    public async Task InterruptOwner(CancellationToken ct)
    {
        var terminate = new NpgsqlCommand("""
            SELECT pg_catalog.pg_terminate_backend(pid, 5000) FROM pg_catalog.pg_stat_activity
            WHERE datname = current_database() AND usename = $1;
            """);
        terminate.Parameters.Add(new NpgsqlParameter<string> { TypedValue = Owner });
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
                CREATE FUNCTION pg_temp.drop_migration_fixture_roles(owner_role text, verifier_role text, data_role text, runtime_role text, branding_owner text, branding_verifier text, branding_data text)
                RETURNS void LANGUAGE plpgsql SET search_path = pg_catalog, pg_temp AS $fixture$
                BEGIN
                    EXECUTE pg_catalog.format('DROP ROLE IF EXISTS %I', branding_data);
                    EXECUTE pg_catalog.format('DROP ROLE IF EXISTS %I', branding_verifier);
                    EXECUTE pg_catalog.format('DROP ROLE IF EXISTS %I', branding_owner);
                    EXECUTE pg_catalog.format('DROP ROLE IF EXISTS %I', runtime_role);
                    EXECUTE pg_catalog.format('DROP ROLE IF EXISTS %I', data_role);
                    EXECUTE pg_catalog.format('DROP ROLE IF EXISTS %I', verifier_role);
                    EXECUTE pg_catalog.format('DROP ROLE IF EXISTS %I', owner_role);
                END;
                $fixture$;
                """);
            var drop = new NpgsqlBatchCommand("SELECT pg_temp.drop_migration_fixture_roles($1, $2, $3, $4, $5, $6, $7);");
            drop.Parameters.Add(new NpgsqlParameter<string> { TypedValue = Owner });
            drop.Parameters.Add(new NpgsqlParameter<string> { TypedValue = Verifier });
            drop.Parameters.Add(new NpgsqlParameter<string> { TypedValue = DataMigrator });
            drop.Parameters.Add(new NpgsqlParameter<string> { TypedValue = Runtime });
            drop.Parameters.Add(new NpgsqlParameter<string> { TypedValue = BrandingOwner });
            drop.Parameters.Add(new NpgsqlParameter<string> { TypedValue = BrandingVerifier });
            drop.Parameters.Add(new NpgsqlParameter<string> { TypedValue = BrandingDataMigrator });
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

    private static async Task<NpgsqlConnection> Open(string connectionString, CancellationToken ct)
    {
        var connection = new NpgsqlConnection(connectionString);
        try
        {
            await connection.OpenAsync(ct);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

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
