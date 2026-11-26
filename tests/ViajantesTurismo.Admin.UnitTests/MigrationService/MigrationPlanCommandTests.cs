using System.Text.Json.Nodes;

namespace ViajantesTurismo.Admin.UnitTests.MigrationService;

public sealed class MigrationPlanCommandTests
{
    [Fact]
    public async Task Plan_reports_verified_phases_and_recovery()
    {
        // Arrange
        using var fixture = new MigrationPlanFixture();

        // Act
        var (exitCode, output, error) = await fixture.Run(TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(0);
        output.ShouldContain("admin/admin-001", StringComparison.Ordinal);
        output.ShouldContain("expand 1 atomic", StringComparison.Ordinal);
        output.ShouldContain("verify 1 atomic", StringComparison.Ordinal);
        output.ShouldContain("recovery restore", StringComparison.Ordinal);
        output.ShouldNotContain("CREATE TABLE", StringComparison.Ordinal);
        error.ShouldBeEmpty();
    }

    [Fact]
    public async Task Plan_accepts_a_batched_step_with_a_checkpoint_and_verified_postcondition()
    {
        // Arrange
        using var fixture = new MigrationPlanFixture();
        var steps = fixture.Manifest["steps"].ShouldNotBeNull().AsArray();
        var batch = steps[0].ShouldNotBeNull().DeepClone();
        var verify = steps[1].ShouldNotBeNull();
        batch["phase"] = "data";
        batch["transaction"] = "batched";
        batch["checkpoint"] = "booking-id";
        batch["verify"] = new JsonObject
        {
            ["path"] = verify["script"]?.DeepClone(),
            ["sha256"] = verify["sha256"]?.DeepClone(),
        };
        steps.Insert(1, batch);
        fixture.Manifest["omittedPhases"].ShouldNotBeNull().AsArray().RemoveAt(0);

        // Act
        var (exitCode, output, error) = await fixture.Run(TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(0);
        output.ShouldContain("data 1 batched", StringComparison.Ordinal);
        error.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("unsupported", "Source release is outside the manifest window.")]
    [InlineData("version", "Invalid migration identity or manifest version.")]
    [InlineData("source-duplicate", "Source releases must be valid and unique.")]
    [InlineData("digest", "Artifact digest mismatch.")]
    [InlineData("digest-format", "Artifact digest must be a SHA-256 value.")]
    [InlineData("path", "Artifact path must stay inside the manifest directory.")]
    [InlineData("missing-file", "Cannot read the manifest or one of its artifacts.")]
    [InlineData("script-format", "A migration script must have the .sql extension.")]
    [InlineData("order", "Migration steps must be strictly ordered by phase and order.")]
    [InlineData("omission", "Every phase must have steps or an explicit omission.")]
    [InlineData("verification", "A migration requires a verification step.")]
    [InlineData("destructive", "Destructive changes belong to the contract phase.")]
    [InlineData("transaction", "A batched data step requires a checkpoint identifier.")]
    [InlineData("postcondition", "Batched and online steps require a verification script.")]
    [InlineData("timeout", "Step and lock timeouts must be positive")]
    [InlineData("recovery", "Script-based recovery requires a script.")]
    [InlineData("extra-field", "Invalid migration manifest JSON.")]
    public async Task Plan_rejects_invalid_inputs_before_reporting_success(string change, string expectedError)
    {
        // Arrange
        using var fixture = new MigrationPlanFixture();
        var steps = fixture.Manifest["steps"].ShouldNotBeNull().AsArray();
        var first = steps[0].ShouldNotBeNull();
        var source = "2026.11.1";
        switch (change)
        {
            case "unsupported":
                source = "2026.10.1";
                break;
            case "version":
                fixture.Manifest["schemaVersion"] = 2;
                break;
            case "source-duplicate":
                fixture.Manifest["sourceReleases"].ShouldNotBeNull().AsArray().Add(source);
                break;
            case "digest":
                first["sha256"] = new string('0', 64);
                break;
            case "digest-format":
                first["sha256"] = "invalid";
                break;
            case "path":
                first["script"] = "../outside.sql";
                break;
            case "missing-file":
                first["script"] = "missing.sql";
                break;
            case "script-format":
                first["script"] = "notes.md";
                break;
            case "order":
                first["phase"] = "verify";
                break;
            case "omission":
                fixture.Manifest["omittedPhases"].ShouldNotBeNull().AsArray().RemoveAt(0);
                break;
            case "verification":
                steps.RemoveAt(1);
                break;
            case "destructive":
                first["destructive"] = true;
                break;
            case "transaction":
                first["transaction"] = "batched";
                break;
            case "postcondition":
                first["transaction"] = "online";
                break;
            case "timeout":
                first["lockTimeoutSeconds"] = 31;
                break;
            case "recovery":
                fixture.Manifest["recovery"].ShouldNotBeNull()["kind"] = "reversible";
                break;
            case "extra-field":
                fixture.Manifest["unknown"] = true;
                break;
        }

        // Act
        var (exitCode, output, error) = await fixture.Run(TestContext.Current.CancellationToken, source);

        // Assert
        exitCode.ShouldBe(1);
        output.ShouldBeEmpty();
        error.ShouldContain(expectedError, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("\n")]
    public async Task Plan_accepts_the_single_concurrent_command_for_the_declared_index(string trailer)
    {
        // Arrange
        using var fixture = new MigrationPlanFixture();
        const string sql = """CREATE UNIQUE INDEX CONCURRENTLY "entry_lookup" ON "migration_fixture"."entries" USING btree ("key", "version");""";
        _ = fixture.SetOnlineIndex(sql + trailer);

        // Act
        var (exitCode, output, error) = await fixture.Run(TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(0);
        error.ShouldBeEmpty();
        output.ShouldContain("expand 1 online", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Plan_requires_the_exact_declared_expression_index_command(bool appendStatement)
    {
        // Arrange
        using var fixture = new MigrationPlanFixture();
        const string sql = """CREATE INDEX CONCURRENTLY "entry_lookup" ON "migration_fixture"."entries" USING btree """
            + """("key", ("payload" ->> 'IntentId'), "version") INCLUDE ("recorded_at") """
            + """WHERE "event_type" IN ('catalog.changed.v1', 'catalog.rejected.v1');""";
        var step = fixture.SetOnlineIndex(appendStatement ? sql + "\nSELECT true;" : sql);
        step["onlineIndex"] = JsonNode.Parse("""
            {
                "schema": "migration_fixture", "table": "entries", "name": "entry_lookup",
                "keys": [{ "column": "key" }, { "column": "payload", "jsonProperty": "IntentId" }, { "column": "version" }],
                "include": ["recorded_at"],
                "textFilter": { "column": "event_type", "values": ["catalog.changed.v1", "catalog.rejected.v1"] },
                "unique": false
            }
            """);

        // Act
        var (exitCode, output, error) = await fixture.Run(TestContext.Current.CancellationToken);

        // Assert
        if (appendStatement)
        {
            exitCode.ShouldBe(1);
            output.ShouldBeEmpty();
            error.ShouldContain("single concurrent index creation", StringComparison.Ordinal);
        }
        else
        {
            exitCode.ShouldBe(0);
            error.ShouldBeEmpty();
            output.ShouldContain("expand 1 online", StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData("""{ "keys": [{ "column": "key" }], "columns": ["key"] }""")]
    [InlineData("""{ "keys": [{ "column": "payload", "jsonProperty": "two words" }] }""")]
    [InlineData("""{ "keys": [{ "column": "key" }, { "column": "key" }] }""")]
    [InlineData("""{ "keys": [{ "column": "key" }], "include": ["key"] }""")]
    [InlineData("""{ "keys": [{ "column": "key" }], "textFilter": { "column": "event_type", "values": [] } }""")]
    [InlineData("""{ "keys": [{ "column": "key" }], "textFilter": { "column": "event_type", "values": ["two words"] } }""")]
    public async Task Plan_rejects_invalid_typed_index_parts(string declaration)
    {
        // Arrange
        using var fixture = new MigrationPlanFixture();
        var step = fixture.SetOnlineIndex("""CREATE INDEX CONCURRENTLY "entry_lookup" ON "migration_fixture"."entries" USING btree ("key");""");
        var index = JsonNode.Parse(declaration).ShouldNotBeNull().AsObject();
        index["schema"] = "migration_fixture";
        index["table"] = "entries";
        index["name"] = "entry_lookup";
        index["unique"] = false;
        step["onlineIndex"] = index;

        // Act
        var (exitCode, output, error) = await fixture.Run(TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(1);
        output.ShouldBeEmpty();
        error.ShouldContain("Invalid online index declaration", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("missing", "Online execution requires a declared index")]
    [InlineData("atomic", "An online index declaration requires online transaction mode")]
    [InlineData("checkpoint", "Online execution requires a declared index")]
    [InlineData("schema", "Invalid online index declaration")]
    [InlineData("duplicate", "Invalid online index declaration")]
    [InlineData("empty", "Invalid online index declaration")]
    [InlineData("sql", "single concurrent index creation")]
    public async Task Plan_rejects_an_incomplete_or_mismatched_index_declaration(string change, string expectedError)
    {
        // Arrange
        using var fixture = new MigrationPlanFixture();
        const string sql = """CREATE UNIQUE INDEX CONCURRENTLY "entry_lookup" ON "migration_fixture"."entries" USING btree ("key", "version");""";
        var step = fixture.SetOnlineIndex(change == "sql" ? sql + "\nSELECT true;" : sql);
        var index = step["onlineIndex"].ShouldNotBeNull().AsObject();
        switch (change)
        {
            case "missing":
                _ = step.Remove("onlineIndex");
                break;
            case "atomic":
                step["transaction"] = "atomic";
                break;
            case "checkpoint":
                step["checkpoint"] = "progress";
                break;
            case "schema":
                index["schema"] = "Two Words";
                break;
            case "duplicate":
                index["columns"] = new JsonArray("key", "key");
                break;
            case "empty":
                index["columns"] = new JsonArray();
                break;
        }

        // Act
        var (exitCode, output, error) = await fixture.Run(TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(1);
        error.ShouldContain(expectedError, StringComparison.Ordinal);
        output.ShouldBeEmpty();
    }

    [Fact]
    public async Task Plan_reports_the_reviewed_recovery_script_and_verifier_digests()
    {
        // Arrange
        using var fixture = new MigrationPlanFixture();
        var recovery = fixture.Manifest["recovery"].ShouldNotBeNull().AsObject();
        var steps = fixture.Manifest["steps"].ShouldNotBeNull().AsArray();
        var scriptDigest = steps[0].ShouldNotBeNull()["sha256"].ShouldNotBeNull().GetValue<string>();
        var verificationDigest = steps[1].ShouldNotBeNull()["sha256"].ShouldNotBeNull().GetValue<string>();
        recovery["kind"] = "reversible";
        recovery["script"] = new JsonObject { ["path"] = "expand.sql", ["sha256"] = scriptDigest };
        recovery["verify"] = new JsonObject { ["path"] = "verify.sql", ["sha256"] = verificationDigest };

        // Act
        var (exitCode, output, error) = await fixture.Run(TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(0);
        error.ShouldBeEmpty();
        output.ShouldContain($"recovery script {scriptDigest}", StringComparison.Ordinal);
        output.ShouldContain($"recovery verify {verificationDigest}", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Plan_reports_the_explicit_canonical_journal_schema()
    {
        // Arrange
        using var fixture = new MigrationPlanFixture();
        fixture.Manifest["journalSchema"] = "governed_migration_admin";

        // Act
        var (exitCode, output, error) = await fixture.Run(TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(0);
        error.ShouldBeEmpty();
        output.ShouldContain("journal governed_migration_admin", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("admin", "governed_migration_catalog")]
    [InlineData("Admin", "governed_migration_Admin")]
    [InlineData("admin.region", "governed_migration_admin.region")]
    [InlineData("admin", "governed_migration")]
    [InlineData("context_with_a_postgresql_name_that_is_too_long_for_the_prefix", "governed_migration_context_with_a_postgresql_name_that_is_too_long_for_the_prefix")]
    public async Task Plan_rejects_a_journal_schema_outside_the_context_binding(string context, string schema)
    {
        // Arrange
        using var fixture = new MigrationPlanFixture();
        fixture.Manifest["context"] = context;
        fixture.Manifest["journalSchema"] = schema;

        // Act
        var (exitCode, output, error) = await fixture.Run(TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(1);
        output.ShouldBeEmpty();
        error.ShouldContain("journal schema must be governed_migration_ followed by the lowercase context identifier", StringComparison.Ordinal);
    }
}
