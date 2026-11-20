using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using PandaBear.Testing;
using ViajantesTurismo.MigrationService.GovernedMigrations;

namespace ViajantesTurismo.Admin.UnitTests.MigrationService;

/// <summary>Creates a versioned manifest, synthetic SQL, and recovery evidence in an isolated directory.</summary>
internal sealed class MigrationPlanFixture : IDisposable
{
    private const string Expansion = "CREATE TABLE synthetic_migration (id integer PRIMARY KEY);\n";
    private const string Verify = "SELECT to_regclass('synthetic_migration') IS NOT NULL;\n";
    private const string Evidence = "Synthetic restore rehearsal completed.\n";
    private readonly TemporaryDirectory directory = new();

    /// <summary>Writes the valid baseline artifacts and prepares the manifest.</summary>
    internal MigrationPlanFixture()
    {
        File.WriteAllText(Path.Combine(directory.Path, "expand.sql"), Expansion);
        File.WriteAllText(Path.Combine(directory.Path, "verify.sql"), Verify);
        File.WriteAllText(Path.Combine(directory.Path, "recovery.md"), Evidence);
        Manifest = JsonNode.Parse(
            $$"""
                {
                  "schemaVersion": 1,
                  "migrationId": "admin-001",
                  "context": "admin",
                  "sourceReleases": ["2026.11.1"],
                  "targetRelease": "2026.11.2",
                  "steps": [
                    {
                      "phase": "expand",
                      "order": 1,
                      "script": "expand.sql",
                      "sha256": "{{Digest(Expansion)}}",
                      "transaction": "atomic",
                      "timeoutSeconds": 30,
                      "lockTimeoutSeconds": 5,
                      "destructive": false
                    },
                    {
                      "phase": "verify",
                      "order": 1,
                      "script": "verify.sql",
                      "sha256": "{{Digest(Verify)}}",
                      "transaction": "atomic",
                      "timeoutSeconds": 30,
                      "lockTimeoutSeconds": 5,
                      "destructive": false
                    }
                  ],
                  "omittedPhases": [
                    {"phase": "data", "reason": "Empty table"},
                    {"phase": "contract", "reason": "Retain the existing contract"}
                  ],
                  "recovery": {
                    "kind": "restore",
                    "evidence": {"path": "recovery.md", "sha256": "{{Digest(Evidence)}}"}
                  }
                }
                """).ShouldNotBeNull().AsObject();
    }

    /// <summary>Gets the editable manifest used for the next invocation.</summary>
    internal JsonObject Manifest
    {
        get;
    }

    /// <summary>Runs the plan command and captures its status and output.</summary>
    internal async Task<(int ExitCode, string Output, string Error)> Run(
        CancellationToken ct, string sourceRelease = "2026.11.1")
    {
        var manifestPath = Path.Combine(directory.Path, "manifest.json");
        await File.WriteAllTextAsync(manifestPath, Manifest.ToJsonString(JsonSerializerOptions.Web), ct);
        using var output = new InvariantStringWriter();
        using var error = new InvariantStringWriter();
        var exitCode = await MigrationPlanCommand.Run(["plan", manifestPath, "--from", sourceRelease], output, error, ct);
        return (exitCode, output.ToString(), error.ToString());
    }

    /// <inheritdoc />
    public void Dispose() => directory.Dispose();

    private static string Digest(string text) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
}
