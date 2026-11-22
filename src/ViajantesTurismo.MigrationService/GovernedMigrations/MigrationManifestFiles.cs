using System.Text.Json;
using System.Text.Json.Serialization;
using PandaBear.IO;

namespace ViajantesTurismo.MigrationService.GovernedMigrations;

/// <summary>Reads the manifest used by planning and execution.</summary>
internal static class MigrationManifestFiles
{
    private const int ManifestLimit = 1024 * 1024;

    private static readonly JsonSerializerOptions ManifestOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        AllowDuplicateProperties = false,
        Converters =
        {
            new JsonStringEnumConverter<MigrationPhase>(JsonNamingPolicy.CamelCase, allowIntegerValues: false),
            new JsonStringEnumConverter<MigrationTransaction>(JsonNamingPolicy.CamelCase, allowIntegerValues: false),
            new JsonStringEnumConverter<MigrationRecoveryKind>(JsonNamingPolicy.CamelCase, allowIntegerValues: false),
        },
    };

    private static readonly MigrationArtifactJsonContext JsonContext = new(ManifestOptions);

    /// <summary>Validates the manifest and records the digest of its unmodified bytes.</summary>
    internal static async Task<(MigrationManifest Manifest, string Directory, string Digest)> Read(
        string manifestPath, string sourceRelease, CancellationToken ct)
    {
        var result = await ArtifactManifest.Read(manifestPath, ManifestLimit, JsonContext.MigrationManifest, ct);
        MigrationManifestValidator.Validate(result.Manifest, sourceRelease);
        return result;
    }

    /// <summary>Deserializes a receipt using the original strict manifest property rules.</summary>
    internal static MigrationRetirementReceipt DeserializeReceipt(ReadOnlySpan<byte> content) =>
        JsonSerializer.Deserialize(content, JsonContext.MigrationRetirementReceipt)
        ?? throw new InvalidDataException("A governed migration JSON artifact must be an object.");

    /// <summary>Checks the evidence and any declared recovery scripts without executing them.</summary>
    internal static async Task VerifyRecovery(MigrationRecovery recovery, string directory, CancellationToken ct)
    {
        await MigrationArtifactFiles.Verify(directory, recovery.Evidence, ct);
        if (recovery.Script is { } script)
        {
            _ = await MigrationArtifactFiles.ReadVerifiedSql(directory, script, ct);
        }

        if (recovery.Verify is { } verification)
        {
            _ = await MigrationArtifactFiles.ReadVerifiedSql(directory, verification, ct);
        }
    }
}
