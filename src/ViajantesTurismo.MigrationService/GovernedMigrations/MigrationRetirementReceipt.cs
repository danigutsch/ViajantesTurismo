using System.Text.Json;

namespace ViajantesTurismo.MigrationService.GovernedMigrations;

/// <summary>Binds a protected deployment's retirement observation to one governed contract.</summary>
/// <param name="SchemaVersion">The receipt format version.</param>
/// <param name="Context">The affected bounded context.</param>
/// <param name="MigrationId">The governed migration identity.</param>
/// <param name="ManifestSha256">The digest of the exact reviewed manifest.</param>
/// <param name="TargetRelease">The application release prepared by the migration.</param>
/// <param name="RetiredSourceReleases">The previous releases that the deployment has retired.</param>
/// <param name="DeploymentTarget">The protected deployment target identifier.</param>
/// <param name="Database">The database selected for that target.</param>
/// <param name="RecoveryEvidenceSha256">The verified recovery rehearsal evidence digest.</param>
/// <param name="RecoveryRehearsalPassed">Whether the protected deployment verified a successful rehearsal.</param>
internal sealed record MigrationRetirementReceipt(
    int SchemaVersion, string Context, string MigrationId, string ManifestSha256, string TargetRelease,
    string[] RetiredSourceReleases, string DeploymentTarget, string Database,
    string RecoveryEvidenceSha256, bool RecoveryRehearsalPassed)
{
    private const int ReceiptLimit = 64 * 1024;

    /// <summary>Verifies the bounded receipt and its release, rehearsal, and deployment target.</summary>
    internal static async Task Validate(string path, string digest, MigrationManifest manifest, string manifestDigest,
        string database, string? protectedTarget, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(protectedTarget))
        {
            throw new InvalidDataException("A protected deployment target is required for contract execution.");
        }

        var file = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(file) ?? throw new InvalidDataException("A retirement receipt directory is required.");
        var artifact = new MigrationArtifact(Path.GetFileName(file), digest);
        var bytes = await MigrationArtifactFiles.ReadVerifiedBytes(directory, artifact, ReceiptLimit, ct);
        MigrationRetirementReceipt receipt;
        try
        {
            receipt = MigrationManifestFiles.DeserializeReceipt(bytes.Span);
        }
        catch (JsonException)
        {
            throw new InvalidDataException("Invalid retirement receipt JSON.");
        }

        if (receipt.SchemaVersion != 1 || !receipt.RecoveryRehearsalPassed
            || !string.Equals(receipt.Context, manifest.Context, StringComparison.Ordinal)
            || !string.Equals(receipt.MigrationId, manifest.MigrationId, StringComparison.Ordinal)
            || !string.Equals(receipt.ManifestSha256, manifestDigest, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(receipt.TargetRelease, manifest.TargetRelease, StringComparison.Ordinal)
            || !string.Equals(receipt.DeploymentTarget, protectedTarget, StringComparison.Ordinal)
            || !string.Equals(receipt.Database, database, StringComparison.Ordinal)
            || !string.Equals(receipt.RecoveryEvidenceSha256, manifest.Recovery.Evidence.Sha256, StringComparison.OrdinalIgnoreCase)
            || receipt.RetiredSourceReleases.Length != manifest.SourceReleases.Length
            || !receipt.RetiredSourceReleases.ToHashSet(StringComparer.Ordinal).SetEquals(manifest.SourceReleases))
        {
            throw new InvalidDataException("Retirement evidence does not match the reviewed releases, target, or recovery rehearsal.");
        }
    }
}
