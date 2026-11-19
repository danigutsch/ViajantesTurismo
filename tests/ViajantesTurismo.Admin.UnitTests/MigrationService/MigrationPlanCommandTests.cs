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
}
