using System.Globalization;
using System.Text.Json;

namespace ViajantesTurismo.MigrationService.GovernedMigrations;

/// <summary>Validates the release artifacts and prints the ordered database migration plan.</summary>
internal static class MigrationPlanCommand
{
    /// <summary>Runs the migration planning command.</summary>
    internal static async Task<int> Run(string[] args, TextWriter output, TextWriter error, CancellationToken ct)
    {
        if (args is not ["plan", var manifestPath, "--from", var sourceRelease] || string.IsNullOrWhiteSpace(manifestPath))
        {
            await error.WriteLineAsync("Usage: plan <manifest> --from <source-release>".AsMemory(), ct);
            return 2;
        }

        try
        {
            var (manifest, directory, _) = await MigrationManifestFiles.Read(manifestPath, sourceRelease, ct);
            await VerifyArtifacts(manifest, directory, ct);
            await WritePlan(manifest, sourceRelease, output, ct);
            return 0;
        }
        catch (InvalidDataException exception)
        {
            await error.WriteLineAsync(exception.Message.AsMemory(), ct);
            return 1;
        }
        catch (JsonException)
        {
            await error.WriteLineAsync("Invalid migration manifest JSON.".AsMemory(), ct);
            return 1;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            await error.WriteLineAsync("Cannot read the manifest or one of its artifacts.".AsMemory(), ct);
            return 1;
        }
    }

    private static async Task VerifyArtifacts(MigrationManifest manifest, string directory, CancellationToken ct)
    {
        foreach (var step in manifest.Steps)
        {
            await MigrationArtifactFiles.VerifySql(directory, new MigrationArtifact(step.Script, step.Sha256), ct);
            if (step.Verify is { } postcondition)
            {
                await MigrationArtifactFiles.VerifySql(directory, postcondition, ct);
            }
        }

        await MigrationManifestFiles.VerifyRecovery(manifest.Recovery, directory, ct);
    }

    private static async Task WritePlan(MigrationManifest manifest, string sourceRelease, TextWriter output, CancellationToken ct)
    {
        await output.WriteLineAsync($"{manifest.Context}/{manifest.MigrationId} {sourceRelease} -> {manifest.TargetRelease}".AsMemory(), ct);
        foreach (var step in manifest.Steps)
        {
            var phase = JsonNamingPolicy.CamelCase.ConvertName(step.Phase.ToString());
            var transaction = JsonNamingPolicy.CamelCase.ConvertName(step.Transaction.ToString());
            var order = step.Order.ToString(CultureInfo.InvariantCulture);
            await output.WriteLineAsync($"{phase} {order} {transaction} {step.Sha256}".AsMemory(), ct);
        }

        var recovery = JsonNamingPolicy.CamelCase.ConvertName(manifest.Recovery.Kind.ToString());
        await output.WriteLineAsync($"recovery {recovery} {manifest.Recovery.Evidence.Sha256}".AsMemory(), ct);
    }
}
